using ShopeeClone.Application.Cart.Dtos;
using ShopeeClone.Application.Common;

namespace ShopeeClone.Application.Cart.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<ServiceResult<CartDto>> AddItemAsync(string userId, AddCartItemRequest request);
    Task<ServiceResult<CartDto>> UpdateItemAsync(string userId, Guid cartItemId, UpdateCartItemRequest request);
    Task<ServiceResult<CartDto>> RemoveItemAsync(string userId, Guid cartItemId);
}
