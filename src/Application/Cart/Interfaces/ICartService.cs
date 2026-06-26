using HungStore.Application.Cart.Dtos;
using HungStore.Application.Common;

namespace HungStore.Application.Cart.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<ServiceResult<CartDto>> AddItemAsync(string userId, AddCartItemRequest request);
    Task<ServiceResult<CartDto>> UpdateItemAsync(string userId, Guid cartItemId, UpdateCartItemRequest request);
    Task<ServiceResult<CartDto>> RemoveItemAsync(string userId, Guid cartItemId);
}
