using FluentAssertions;
using Moq;
using ShopeeClone.Application.Auth.Dtos;
using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Application.Notifications.Interfaces;
using ShopeeClone.Application.Orders;
using ShopeeClone.Application.Orders.Dtos;
using ShopeeClone.Application.Vouchers.Dtos;
using ShopeeClone.Application.Vouchers.Interfaces;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Enums;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.UnitTests.Orders;

public class OrderServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IVoucherService> _voucherServiceMock = new();
    private readonly Mock<IFlashSaleRepository> _flashSaleRepositoryMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _flashSaleRepositoryMock
            .Setup(x => x.GetActiveItemsForProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Dictionary<Guid, FlashSaleItem>());
        _productRepositoryMock
            .Setup(x => x.GetImageUrlsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string?>());
        _orderRepositoryMock
            .Setup(x => x.GetStatusHistoryAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<OrderStatusHistory>());

        _sut = new OrderService(
            _cartRepositoryMock.Object,
            _productRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _identityServiceMock.Object,
            _notificationServiceMock.Object,
            _voucherServiceMock.Object,
            _flashSaleRepositoryMock.Object);
    }

    private static readonly CreateOrderRequest DefaultRequest = new()
    {
        ShippingAddress = "123 Lê Lợi, Q1, TP.HCM",
        PaymentMethod = PaymentMethod.Cod
    };

    [Fact]
    public async Task CreateOrderFromCartAsync_EmptyCart_ReturnsFailure()
    {
        var cart = new Domain.Entities.Cart { Id = Guid.NewGuid(), UserId = "user-1", Items = new List<CartItem>() };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("trống"));
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_NoCartAtAll_ReturnsFailure()
    {
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync((Domain.Entities.Cart?)null);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_QuantityExceedsStock_ReturnsFailure()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 2 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem>
            {
                new() { ProductId = product.Id, Product = product, Quantity = 5 }
            }
        };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("iPhone"));
        _productRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_Success_DeductsStockComputesTotalAndClearsCart()
    {
        var productA = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 10 };
        var productB = new Product { Id = Guid.NewGuid(), Name = "Sạc dự phòng", Price = 50, Stock = 3 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem>
            {
                new() { ProductId = productA.Id, Product = productA, Quantity = 2 },
                new() { ProductId = productB.Id, Product = productB, Quantity = 1 }
            }
        };

        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _productRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _cartRepositoryMock.Setup(x => x.ClearAsync(cart.Id)).Returns(Task.CompletedTask);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(2050); // 2*1000 + 1*50
        result.Data!.Items.Should().HaveCount(2);
        result.Data!.Status.Should().Be(nameof(OrderStatus.Pending));

        productA.Stock.Should().Be(8);
        productB.Stock.Should().Be(2);
        _productRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Exactly(2));
        _orderRepositoryMock.Verify(x => x.AddAsync(It.Is<Order>(o => o.TotalAmount == 2050 && o.Items.Count == 2)), Times.Once);
        _orderRepositoryMock.Verify(x => x.AddStatusHistoryAsync(It.Is<OrderStatusHistory>(h => h.Status == OrderStatus.Pending)), Times.Once);
        _cartRepositoryMock.Verify(x => x.ClearAsync(cart.Id), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_WithVariant_DeductsVariantStockAndSnapshotsVariantDescription()
    {
        var variant = new ProductVariant { Id = Guid.NewGuid(), OptionValue1 = new ProductVariantOptionValue { Value = "Đen" }, Stock = 5 };
        var product = new Product { Id = Guid.NewGuid(), Name = "Smartphone Pro", Price = 1000, Stock = 0, Variants = new List<ProductVariant> { variant } };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem>
            {
                new() { ProductId = product.Id, Product = product, ProductVariantId = variant.Id, ProductVariant = variant, Quantity = 2 }
            }
        };

        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _productRepositoryMock.Setup(x => x.UpdateVariantAsync(It.IsAny<ProductVariant>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _cartRepositoryMock.Setup(x => x.ClearAsync(cart.Id)).Returns(Task.CompletedTask);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Single().VariantDescription.Should().Be("Đen");
        variant.Stock.Should().Be(3);
        _productRepositoryMock.Verify(x => x.UpdateVariantAsync(It.Is<ProductVariant>(v => v.Stock == 3)), Times.Once);
        _productRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_QuantityExceedsVariantStock_ReturnsFailure()
    {
        var variant = new ProductVariant { Id = Guid.NewGuid(), OptionValue1 = new ProductVariantOptionValue { Value = "Đen" }, Stock = 1 };
        var product = new Product { Id = Guid.NewGuid(), Name = "Smartphone Pro", Price = 1000, Variants = new List<ProductVariant> { variant } };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem>
            {
                new() { ProductId = product.Id, Product = product, ProductVariantId = variant.Id, ProductVariant = variant, Quantity = 3 }
            }
        };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Đen"));
        _productRepositoryMock.Verify(x => x.UpdateVariantAsync(It.IsAny<ProductVariant>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_TwoDimensionVariant_SnapshotsCombinedVariantDescription()
    {
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            OptionValue1 = new ProductVariantOptionValue { Value = "Đen" },
            OptionValue2 = new ProductVariantOptionValue { Value = "256GB" },
            Stock = 5
        };
        var product = new Product { Id = Guid.NewGuid(), Name = "Smartphone Pro", Price = 1000, Stock = 0, Variants = new List<ProductVariant> { variant } };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem>
            {
                new() { ProductId = product.Id, Product = product, ProductVariantId = variant.Id, ProductVariant = variant, Quantity = 1 }
            }
        };

        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _productRepositoryMock.Setup(x => x.UpdateVariantAsync(It.IsAny<ProductVariant>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _cartRepositoryMock.Setup(x => x.ClearAsync(cart.Id)).Returns(Task.CompletedTask);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Single().VariantDescription.Should().Be("Đen, 256GB");
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_MockPaid_SetsIsPaidTrue()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 10 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem> { new() { ProductId = product.Id, Product = product, Quantity = 1 } }
        };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);

        var result = await _sut.CreateOrderFromCartAsync(
            "user-1",
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.MockPaid });

        result.Succeeded.Should().BeTrue();
        result.Data!.IsPaid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_WithValidVoucher_AppliesDiscountAndRedeemsVoucher()
    {
        var voucherId = Guid.NewGuid();
        var product = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 10 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem> { new() { ProductId = product.Id, Product = product, Quantity = 1 } }
        };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _voucherServiceMock
            .Setup(x => x.ValidateAsync("SALE10", "user-1", 1000))
            .ReturnsAsync(ServiceResult<VoucherValidationResultDto>.Success(new VoucherValidationResultDto
            {
                VoucherId = voucherId,
                Code = "SALE10",
                Subtotal = 1000,
                DiscountAmount = 100,
                FinalTotal = 900
            }));

        var result = await _sut.CreateOrderFromCartAsync(
            "user-1",
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod, VoucherCode = "SALE10" });

        result.Succeeded.Should().BeTrue();
        result.Data!.Subtotal.Should().Be(1000);
        result.Data!.DiscountAmount.Should().Be(100);
        result.Data!.TotalAmount.Should().Be(900);
        _voucherServiceMock.Verify(x => x.RedeemAsync(voucherId, "user-1", result.Data!.Id), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_WithInvalidVoucher_RejectsOrder()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 10 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem> { new() { ProductId = product.Id, Product = product, Quantity = 1 } }
        };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _voucherServiceMock
            .Setup(x => x.ValidateAsync("EXPIRED", "user-1", 1000))
            .ReturnsAsync(ServiceResult<VoucherValidationResultDto>.Failure("Mã giảm giá đã hết hạn."));

        var result = await _sut.CreateOrderFromCartAsync(
            "user-1",
            new CreateOrderRequest { ShippingAddress = "addr", PaymentMethod = PaymentMethod.Cod, VoucherCode = "EXPIRED" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("hết hạn"));
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ProductInActiveFlashSale_SnapshotsSalePriceAndIncrementsQuantitySold()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 10 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem> { new() { ProductId = product.Id, Product = product, Quantity = 2 } }
        };
        var flashSaleItem = new FlashSaleItem { Id = Guid.NewGuid(), ProductId = product.Id, SalePrice = 700, QuantityLimit = 10, QuantitySold = 0 };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _flashSaleRepositoryMock
            .Setup(x => x.GetActiveItemsForProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Dictionary<Guid, FlashSaleItem> { [product.Id] = flashSaleItem });
        _flashSaleRepositoryMock.Setup(x => x.TryIncrementQuantitySoldAsync(flashSaleItem.Id, 2)).ReturnsAsync(true);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Single().UnitPrice.Should().Be(700);
        result.Data!.TotalAmount.Should().Be(1400);
        _flashSaleRepositoryMock.Verify(x => x.TryIncrementQuantitySoldAsync(flashSaleItem.Id, 2), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_FlashSaleSoldOut_FallsBackToRegularPrice()
    {
        var product = new Product { Id = Guid.NewGuid(), Name = "iPhone", Price = 1000, Stock = 10 };
        var cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<CartItem> { new() { ProductId = product.Id, Product = product, Quantity = 1 } }
        };
        var flashSaleItem = new FlashSaleItem { Id = Guid.NewGuid(), ProductId = product.Id, SalePrice = 700, QuantityLimit = 5, QuantitySold = 5 };
        _cartRepositoryMock.Setup(x => x.GetByUserIdAsync("user-1")).ReturnsAsync(cart);
        _flashSaleRepositoryMock
            .Setup(x => x.GetActiveItemsForProductsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Dictionary<Guid, FlashSaleItem> { [product.Id] = flashSaleItem });
        _flashSaleRepositoryMock.Setup(x => x.TryIncrementQuantitySoldAsync(flashSaleItem.Id, 1)).ReturnsAsync(false);

        var result = await _sut.CreateOrderFromCartAsync("user-1", DefaultRequest);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Single().UnitPrice.Should().Be(1000);
        result.Data!.TotalAmount.Should().Be(1000);
    }

    [Fact]
    public async Task GetMyOrdersAsync_PassesStatusFilterToRepository()
    {
        _orderRepositoryMock
            .Setup(x => x.GetByUserIdPagedAsync("user-1", OrderStatus.Delivered, 1, 10))
            .ReturnsAsync((new List<Order>(), 0));

        await _sut.GetMyOrdersAsync("user-1", OrderStatus.Delivered, 1, 10);

        _orderRepositoryMock.Verify(x => x.GetByUserIdPagedAsync("user-1", OrderStatus.Delivered, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetMyOrdersAsync_IncludesProductImageUrlPerItem()
    {
        var productId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Items = new List<OrderItem> { new() { ProductId = productId, ProductName = "iPhone", UnitPrice = 1000, Quantity = 1 } }
        };
        _orderRepositoryMock
            .Setup(x => x.GetByUserIdPagedAsync("user-1", null, 1, 10))
            .ReturnsAsync((new List<Order> { order }, 1));
        _productRepositoryMock
            .Setup(x => x.GetImageUrlsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string?> { [productId] = "/uploads/products/iphone.jpg" });

        var result = await _sut.GetMyOrdersAsync("user-1", null, 1, 10);

        result.Items.Single().Items.Single().ImageUrl.Should().Be("/uploads/products/iphone.jpg");
    }

    [Fact]
    public async Task GetOrderByIdAsync_IncludesStatusHistoryInChronologicalOrder()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "owner", Status = OrderStatus.Confirmed };
        var pendingAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var confirmedAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _orderRepositoryMock.Setup(x => x.GetStatusHistoryAsync(order.Id)).ReturnsAsync(new List<OrderStatusHistory>
        {
            new() { OrderId = order.Id, Status = OrderStatus.Pending, CreatedAt = pendingAt },
            new() { OrderId = order.Id, Status = OrderStatus.Confirmed, CreatedAt = confirmedAt }
        });

        var result = await _sut.GetOrderByIdAsync("owner", order.Id);

        result.Succeeded.Should().BeTrue();
        result.Data!.StatusHistory.Should().HaveCount(2);
        result.Data!.StatusHistory[0].Status.Should().Be(nameof(OrderStatus.Pending));
        result.Data!.StatusHistory[1].Status.Should().Be(nameof(OrderStatus.Confirmed));
    }

    [Fact]
    public async Task GetOrderByIdAsync_NotOwner_ReturnsFailure()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "owner" };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.GetOrderByIdAsync("someone-else", order.Id);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderByIdAsync_Owner_ReturnsSuccess()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "owner", TotalAmount = 100 };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.GetOrderByIdAsync("owner", order.Id);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(100);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsPagedSummariesWithCustomerEmail()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", TotalAmount = 500, Status = OrderStatus.Pending, Items = new List<OrderItem> { new() } };
        _orderRepositoryMock.Setup(x => x.GetAllPagedAsync(null, 1, 10)).ReturnsAsync((new List<Order> { order }, 1));
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-1")).ReturnsAsync(new UserDto { Id = "user-1", Email = "user@test.com" });

        var result = await _sut.GetAllOrdersAsync(null, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().CustomerEmail.Should().Be("user@test.com");
        result.Items.Single().ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidTransition_UpdatesStatus()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Pending };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _orderRepositoryMock.Setup(x => x.UpdateAsync(order)).Returns(Task.CompletedTask);

        var result = await _sut.UpdateOrderStatusAsync(order.Id, OrderStatus.Confirmed);

        result.Succeeded.Should().BeTrue();
        result.Data!.Status.Should().Be(nameof(OrderStatus.Confirmed));
        _orderRepositoryMock.Verify(x => x.UpdateAsync(order), Times.Once);
        _orderRepositoryMock.Verify(x => x.AddStatusHistoryAsync(It.Is<OrderStatusHistory>(h => h.OrderId == order.Id && h.Status == OrderStatus.Confirmed)), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyOrderStatusChangedAsync("user-1", order.Id, OrderStatus.Confirmed), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ReturnsFailure()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Pending };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.UpdateOrderStatusAsync(order.Id, OrderStatus.Delivered);

        result.Succeeded.Should().BeFalse();
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_TerminalStatus_ReturnsFailure()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Delivered };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.UpdateOrderStatusAsync(order.Id, OrderStatus.Cancelled);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_OrderNotFound_ReturnsFailure()
    {
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        var result = await _sut.UpdateOrderStatusAsync(Guid.NewGuid(), OrderStatus.Confirmed);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShippedToDelivered_ReturnsFailure_BecauseOnlyCustomerOrAutoSweepCanDoIt()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Shipped };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.UpdateOrderStatusAsync(order.Id, OrderStatus.Delivered);

        result.Succeeded.Should().BeFalse();
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_OwnerWithShippedOrder_TransitionsToDelivered()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Shipped };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.ConfirmDeliveryAsync("user-1", order.Id);

        result.Succeeded.Should().BeTrue();
        result.Data!.Status.Should().Be(nameof(OrderStatus.Delivered));
        _orderRepositoryMock.Verify(x => x.AddStatusHistoryAsync(It.Is<OrderStatusHistory>(h => h.OrderId == order.Id && h.Status == OrderStatus.Delivered)), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyOrderStatusChangedAsync("user-1", order.Id, OrderStatus.Delivered), Times.Once);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_NotOwner_ReturnsFailure()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "owner", Status = OrderStatus.Shipped };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.ConfirmDeliveryAsync("intruder", order.Id);

        result.Succeeded.Should().BeFalse();
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmDeliveryAsync_OrderNotYetShipped_ReturnsFailure()
    {
        var order = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Confirmed };
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _sut.ConfirmDeliveryAsync("user-1", order.Id);

        result.Succeeded.Should().BeFalse();
        _orderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task AutoCompleteStaleShippedOrdersAsync_TransitionsEligibleOrdersAndReturnsCount()
    {
        var staleOrder = new Order { Id = Guid.NewGuid(), UserId = "user-1", Status = OrderStatus.Shipped };
        _orderRepositoryMock
            .Setup(x => x.GetOrderIdsEligibleForAutoDeliveryAsync(OrderStatus.Shipped, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Guid> { staleOrder.Id });
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(staleOrder.Id)).ReturnsAsync(staleOrder);

        var completedCount = await _sut.AutoCompleteStaleShippedOrdersAsync(TimeSpan.FromDays(7));

        completedCount.Should().Be(1);
        staleOrder.Status.Should().Be(OrderStatus.Delivered);
        _orderRepositoryMock.Verify(x => x.AddStatusHistoryAsync(It.Is<OrderStatusHistory>(h => h.OrderId == staleOrder.Id && h.Status == OrderStatus.Delivered)), Times.Once);
    }
}
