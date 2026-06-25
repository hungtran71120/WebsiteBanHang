using ShopeeClone.Application.Cart.Dtos;
using ShopeeClone.Application.Cart.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Cart;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;

    public CartService(ICartRepository cartRepository, IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        return MapToDto(cart);
    }

    public async Task<ServiceResult<CartDto>> AddItemAsync(string userId, AddCartItemRequest request)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null)
        {
            return ServiceResult<CartDto>.Failure("Không tìm thấy sản phẩm.");
        }

        ProductVariant? variant = null;
        if (product.Variants.Count > 0)
        {
            if (request.ProductVariantId is null)
            {
                return ServiceResult<CartDto>.Failure("Vui lòng chọn đầy đủ phân loại.");
            }

            variant = product.Variants.FirstOrDefault(v => v.Id == request.ProductVariantId.Value);
            if (variant is null)
            {
                return ServiceResult<CartDto>.Failure("Phân loại đã chọn không tồn tại.");
            }
        }

        var effectiveStock = variant?.Stock ?? product.Stock;
        var label = variant is not null ? $"{product.Name} ({BuildVariantLabel(variant)})" : product.Name;

        var cart = await _cartRepository.GetByUserIdAsync(userId) ?? await _cartRepository.CreateAsync(userId);
        var existingItem = await _cartRepository.GetItemAsync(cart.Id, request.ProductId, variant?.Id);

        var newQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;
        if (newQuantity > effectiveStock)
        {
            return ServiceResult<CartDto>.Failure($"Sản phẩm \"{label}\" chỉ còn {effectiveStock} trong kho.");
        }

        if (existingItem is null)
        {
            await _cartRepository.AddItemAsync(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                ProductVariantId = variant?.Id,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity = newQuantity;
            await _cartRepository.UpdateItemAsync(existingItem);
        }

        return ServiceResult<CartDto>.Success(await GetCartAsync(userId));
    }

    public async Task<ServiceResult<CartDto>> UpdateItemAsync(string userId, Guid cartItemId, UpdateCartItemRequest request)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        var item = cart?.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
        {
            return ServiceResult<CartDto>.Failure("Sản phẩm không có trong giỏ hàng.");
        }

        var effectiveStock = item.ProductVariant?.Stock ?? item.Product?.Stock ?? 0;
        if (request.Quantity > effectiveStock)
        {
            var label = item.ProductVariant is not null
                ? $"{item.Product?.Name} ({BuildVariantLabel(item.ProductVariant)})"
                : item.Product?.Name ?? string.Empty;
            return ServiceResult<CartDto>.Failure($"Sản phẩm \"{label}\" chỉ còn {effectiveStock} trong kho.");
        }

        item.Quantity = request.Quantity;
        await _cartRepository.UpdateItemAsync(item);

        return ServiceResult<CartDto>.Success(await GetCartAsync(userId));
    }

    public async Task<ServiceResult<CartDto>> RemoveItemAsync(string userId, Guid cartItemId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        var item = cart?.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
        {
            return ServiceResult<CartDto>.Failure("Sản phẩm không có trong giỏ hàng.");
        }

        await _cartRepository.RemoveItemAsync(item);

        return ServiceResult<CartDto>.Success(await GetCartAsync(userId));
    }

    internal static string? BuildVariantLabel(ProductVariant? variant)
    {
        if (variant is null)
        {
            return null;
        }

        return variant.OptionValue2 is not null
            ? $"{variant.OptionValue1?.Value}, {variant.OptionValue2.Value}"
            : variant.OptionValue1?.Value;
    }

    private static CartDto MapToDto(Domain.Entities.Cart? cart)
    {
        if (cart is null)
        {
            return new CartDto();
        }

        return new CartDto
        {
            Items = cart.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductImageUrl = i.ProductVariant?.OptionValue1?.ImageUrl ?? i.Product?.ImageUrl,
                ProductVariantId = i.ProductVariantId,
                VariantDescription = BuildVariantLabel(i.ProductVariant),
                UnitPrice = i.Product?.Price ?? 0,
                Stock = i.ProductVariant?.Stock ?? i.Product?.Stock ?? 0,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
