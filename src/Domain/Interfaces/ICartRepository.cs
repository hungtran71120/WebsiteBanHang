using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(string userId);
    Task<Cart> CreateAsync(string userId);
    Task<CartItem?> GetItemAsync(Guid cartId, Guid productId, Guid? variantId);
    Task AddItemAsync(CartItem item);
    Task UpdateItemAsync(CartItem item);
    Task RemoveItemAsync(CartItem item);
    Task ClearAsync(Guid cartId);
}
