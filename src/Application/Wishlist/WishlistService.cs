using HungStore.Application.Common;
using HungStore.Application.Wishlist.Dtos;
using HungStore.Application.Wishlist.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.Wishlist;

public class WishlistService : IWishlistService
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IProductRepository _productRepository;

    public WishlistService(IWishlistRepository wishlistRepository, IProductRepository productRepository)
    {
        _wishlistRepository = wishlistRepository;
        _productRepository = productRepository;
    }

    public async Task<WishlistDto> GetWishlistAsync(string userId)
    {
        var wishlist = await _wishlistRepository.GetByUserIdAsync(userId);
        return MapToDto(wishlist);
    }

    public async Task<ServiceResult<WishlistDto>> AddAsync(string userId, Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<WishlistDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var wishlist = await _wishlistRepository.GetOrCreateAsync(userId);
        var existingItem = await _wishlistRepository.GetItemAsync(wishlist.Id, productId);
        if (existingItem is null)
        {
            await _wishlistRepository.AddItemAsync(new WishlistItem
            {
                WishlistId = wishlist.Id,
                ProductId = productId
            });
        }

        return ServiceResult<WishlistDto>.Success(await GetWishlistAsync(userId));
    }

    public async Task<ServiceResult<WishlistDto>> RemoveAsync(string userId, Guid productId)
    {
        var wishlist = await _wishlistRepository.GetByUserIdAsync(userId);
        var item = wishlist?.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            return ServiceResult<WishlistDto>.Failure("Sản phẩm không có trong danh sách yêu thích.");
        }

        await _wishlistRepository.RemoveItemAsync(item);

        return ServiceResult<WishlistDto>.Success(await GetWishlistAsync(userId));
    }

    private static WishlistDto MapToDto(Domain.Entities.Wishlist? wishlist)
    {
        if (wishlist is null)
        {
            return new WishlistDto();
        }

        return new WishlistDto
        {
            Items = wishlist.Items.Select(i => new WishlistItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductImageUrl = i.Product?.ImageUrl,
                Price = i.Product?.Price ?? 0,
                InStock = (i.Product?.Variants.Count ?? 0) > 0
                    ? i.Product!.Variants.Sum(v => v.Stock) > 0
                    : (i.Product?.Stock ?? 0) > 0
            }).ToList()
        };
    }
}
