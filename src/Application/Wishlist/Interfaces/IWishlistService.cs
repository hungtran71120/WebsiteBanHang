using HungStore.Application.Common;
using HungStore.Application.Wishlist.Dtos;

namespace HungStore.Application.Wishlist.Interfaces;

public interface IWishlistService
{
    Task<WishlistDto> GetWishlistAsync(string userId);
    Task<ServiceResult<WishlistDto>> AddAsync(string userId, Guid productId);
    Task<ServiceResult<WishlistDto>> RemoveAsync(string userId, Guid productId);
}
