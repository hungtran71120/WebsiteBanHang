using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Application.Notifications.Interfaces;
using ShopeeClone.Application.Orders.Dtos;
using ShopeeClone.Application.Orders.Interfaces;
using ShopeeClone.Application.Vouchers.Interfaces;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Enums;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Orders;

public class OrderService : IOrderService
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new Dictionary<OrderStatus, OrderStatus[]>
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
        [OrderStatus.Shipped] = Array.Empty<OrderStatus>(),
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;
    private readonly IVoucherService _voucherService;
    private readonly IFlashSaleRepository _flashSaleRepository;

    public OrderService(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        INotificationService notificationService,
        IVoucherService voucherService,
        IFlashSaleRepository flashSaleRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _identityService = identityService;
        _notificationService = notificationService;
        _voucherService = voucherService;
        _flashSaleRepository = flashSaleRepository;
    }

    public async Task<ServiceResult<OrderDto>> CreateOrderFromCartAsync(string userId, CreateOrderRequest request)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart is null || cart.Items.Count == 0)
        {
            return ServiceResult<OrderDto>.Failure("Giỏ hàng đang trống.");
        }

        foreach (var item in cart.Items)
        {
            if (item.Product is null)
            {
                return ServiceResult<OrderDto>.Failure("Một sản phẩm trong giỏ hàng không còn tồn tại.");
            }

            var effectiveStock = item.ProductVariant?.Stock ?? item.Product.Stock;
            if (item.Quantity > effectiveStock)
            {
                var label = item.ProductVariant is not null
                    ? $"{item.Product.Name} ({BuildVariantLabel(item.ProductVariant)})"
                    : item.Product.Name;
                return ServiceResult<OrderDto>.Failure($"Sản phẩm \"{label}\" chỉ còn {effectiveStock} trong kho.");
            }
        }

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var activeFlashSaleItems = await _flashSaleRepository.GetActiveItemsForProductsAsync(productIds, DateTime.UtcNow);

        var orderItems = new List<OrderItem>();
        foreach (var item in cart.Items)
        {
            var unitPrice = item.Product!.Price;
            if (activeFlashSaleItems.TryGetValue(item.ProductId, out var flashSaleItem))
            {
                var applied = await _flashSaleRepository.TryIncrementQuantitySoldAsync(flashSaleItem.Id, item.Quantity);
                if (applied)
                {
                    unitPrice = flashSaleItem.SalePrice;
                }
            }

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Product!.Name,
                VariantDescription = BuildVariantLabel(item.ProductVariant),
                UnitPrice = unitPrice,
                Quantity = item.Quantity
            });
        }

        var order = new Order
        {
            UserId = userId,
            ShippingAddress = request.ShippingAddress,
            PaymentMethod = request.PaymentMethod,
            IsPaid = request.PaymentMethod == Domain.Enums.PaymentMethod.MockPaid,
            Items = orderItems
        };
        order.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.TotalAmount = order.Subtotal;

        Guid? appliedVoucherId = null;
        if (!string.IsNullOrWhiteSpace(request.VoucherCode))
        {
            var voucherResult = await _voucherService.ValidateAsync(request.VoucherCode, userId, order.Subtotal);
            if (!voucherResult.Succeeded)
            {
                return ServiceResult<OrderDto>.Failure(voucherResult.Errors);
            }

            appliedVoucherId = voucherResult.Data!.VoucherId;
            order.VoucherId = voucherResult.Data.VoucherId;
            order.VoucherCode = voucherResult.Data.Code;
            order.DiscountAmount = voucherResult.Data.DiscountAmount;
            order.TotalAmount = voucherResult.Data.FinalTotal;
        }

        foreach (var item in cart.Items)
        {
            if (item.ProductVariant is not null)
            {
                item.ProductVariant.Stock -= item.Quantity;
                await _productRepository.UpdateVariantAsync(item.ProductVariant);
            }
            else
            {
                item.Product!.Stock -= item.Quantity;
                await _productRepository.UpdateAsync(item.Product);
            }
        }

        await _orderRepository.AddAsync(order);
        await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory { OrderId = order.Id, Status = OrderStatus.Pending });

        if (appliedVoucherId.HasValue)
        {
            await _voucherService.RedeemAsync(appliedVoucherId.Value, userId, order.Id);
        }

        await _cartRepository.ClearAsync(cart.Id);

        return ServiceResult<OrderDto>.Success(await MapToDtoAsync(order));
    }

    public async Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(string userId, OrderStatus? status, int page, int pageSize)
    {
        var (items, totalCount) = await _orderRepository.GetByUserIdPagedAsync(userId, status, page, pageSize);

        var productIds = items.SelectMany(o => o.Items.Select(i => i.ProductId)).Distinct().ToList();
        var imageUrls = await _productRepository.GetImageUrlsAsync(productIds) ?? new Dictionary<Guid, string?>();

        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Count,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ImageUrl = imageUrls.TryGetValue(i.ProductId, out var url) ? url : null,
                    VariantDescription = i.VariantDescription,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<OrderDto>> GetOrderByIdAsync(string userId, Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null || order.UserId != userId)
        {
            return ServiceResult<OrderDto>.Failure("Không tìm thấy đơn hàng.");
        }

        return ServiceResult<OrderDto>.Success(await MapToDtoAsync(order));
    }

    public async Task<PagedResult<AdminOrderSummaryDto>> GetAllOrdersAsync(OrderStatus? status, int page, int pageSize)
    {
        var (items, totalCount) = await _orderRepository.GetAllPagedAsync(status, page, pageSize);

        var summaries = new List<AdminOrderSummaryDto>();
        foreach (var order in items)
        {
            var customer = await _identityService.GetUserByIdAsync(order.UserId);
            summaries.Add(new AdminOrderSummaryDto
            {
                Id = order.Id,
                CustomerEmail = customer?.Email ?? "(đã xóa)",
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                ItemCount = order.Items.Count,
                CreatedAt = order.CreatedAt
            });
        }

        return new PagedResult<AdminOrderSummaryDto>
        {
            Items = summaries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<OrderDto>> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null)
        {
            return ServiceResult<OrderDto>.Failure("Không tìm thấy đơn hàng.");
        }

        if (order.Status == newStatus)
        {
            return ServiceResult<OrderDto>.Success(await MapToDtoAsync(order));
        }

        if (!AllowedTransitions[order.Status].Contains(newStatus))
        {
            return ServiceResult<OrderDto>.Failure($"Không thể chuyển trạng thái từ {order.Status} sang {newStatus}.");
        }

        await TransitionStatusAsync(order, newStatus);

        return ServiceResult<OrderDto>.Success(await MapToDtoAsync(order));
    }

    public async Task<ServiceResult<OrderDto>> ConfirmDeliveryAsync(string userId, Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is null || order.UserId != userId)
        {
            return ServiceResult<OrderDto>.Failure("Không tìm thấy đơn hàng.");
        }

        if (order.Status != OrderStatus.Shipped)
        {
            return ServiceResult<OrderDto>.Failure("Đơn hàng chưa ở trạng thái đang giao, không thể xác nhận đã nhận hàng.");
        }

        await TransitionStatusAsync(order, OrderStatus.Delivered);

        return ServiceResult<OrderDto>.Success(await MapToDtoAsync(order));
    }

    public async Task<int> AutoCompleteStaleShippedOrdersAsync(TimeSpan threshold)
    {
        var cutoff = DateTime.UtcNow - threshold;
        var orderIds = await _orderRepository.GetOrderIdsEligibleForAutoDeliveryAsync(OrderStatus.Shipped, cutoff);

        var completedCount = 0;
        foreach (var orderId in orderIds)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null || order.Status != OrderStatus.Shipped)
            {
                continue;
            }

            await TransitionStatusAsync(order, OrderStatus.Delivered);
            completedCount++;
        }

        return completedCount;
    }

    private async Task TransitionStatusAsync(Order order, OrderStatus newStatus)
    {
        order.Status = newStatus;
        await _orderRepository.UpdateAsync(order);
        await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory { OrderId = order.Id, Status = newStatus });
        await _notificationService.NotifyOrderStatusChangedAsync(order.UserId, order.Id, newStatus);
    }

    private static string? BuildVariantLabel(ProductVariant? variant)
    {
        if (variant is null)
        {
            return null;
        }

        return variant.OptionValue2 is not null
            ? $"{variant.OptionValue1?.Value}, {variant.OptionValue2.Value}"
            : variant.OptionValue1?.Value;
    }

    private async Task<OrderDto> MapToDtoAsync(Order order)
    {
        var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
        var imageUrls = await _productRepository.GetImageUrlsAsync(productIds) ?? new Dictionary<Guid, string?>();
        var statusHistory = await _orderRepository.GetStatusHistoryAsync(order.Id) ?? Array.Empty<OrderStatusHistory>();

        return new OrderDto
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            IsPaid = order.IsPaid,
            ShippingAddress = order.ShippingAddress,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            VoucherCode = order.VoucherCode,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ImageUrl = imageUrls.TryGetValue(i.ProductId, out var url) ? url : null,
                VariantDescription = i.VariantDescription,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList(),
            StatusHistory = statusHistory.Select(h => new OrderStatusHistoryDto
            {
                Status = h.Status.ToString(),
                CreatedAt = h.CreatedAt
            }).ToList()
        };
    }
}
