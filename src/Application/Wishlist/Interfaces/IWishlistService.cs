using ShopeeClone.Application.Common;
using ShopeeClone.Application.Wishlist.Dtos;

namespace ShopeeClone.Application.Wishlist.Interfaces;

public interface IWishlistService
{
    Task<WishlistDto> GetWishlistAsync(string userId);
    Task<ServiceResult<WishlistDto>> AddAsync(string userId, Guid productId);
    Task<ServiceResult<WishlistDto>> RemoveAsync(string userId, Guid productId);
}
