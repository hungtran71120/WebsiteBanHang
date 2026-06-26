using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface IWishlistRepository
{
    Task<Wishlist?> GetByUserIdAsync(string userId);
    Task<Wishlist> GetOrCreateAsync(string userId);
    Task<WishlistItem?> GetItemAsync(Guid wishlistId, Guid productId);
    Task AddItemAsync(WishlistItem item);
    Task RemoveItemAsync(WishlistItem item);
}
