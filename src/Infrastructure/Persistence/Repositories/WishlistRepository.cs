using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class WishlistRepository : IWishlistRepository
{
    private readonly AppDbContext _context;

    public WishlistRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Wishlist?> GetByUserIdAsync(string userId)
    {
        return _context.Wishlists
            .Include(w => w.Items).ThenInclude(wi => wi.Product).ThenInclude(p => p!.Variants)
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<Wishlist> GetOrCreateAsync(string userId)
    {
        var existing = await GetByUserIdAsync(userId);
        if (existing is not null)
        {
            return existing;
        }

        var wishlist = new Wishlist { UserId = userId };
        _context.Wishlists.Add(wishlist);
        try
        {
            await _context.SaveChangesAsync();
            return wishlist;
        }
        catch (DbUpdateException)
        {
            // Another concurrent request from the same user may have already created the
            // wishlist between our check above and this insert (UserId has a unique index).
            _context.Entry(wishlist).State = EntityState.Detached;
            var winner = await GetByUserIdAsync(userId);
            if (winner is null)
            {
                throw;
            }
            return winner;
        }
    }

    public Task<WishlistItem?> GetItemAsync(Guid wishlistId, Guid productId)
    {
        return _context.WishlistItems
            .Include(wi => wi.Product)
            .FirstOrDefaultAsync(wi => wi.WishlistId == wishlistId && wi.ProductId == productId);
    }

    public async Task AddItemAsync(WishlistItem item)
    {
        _context.WishlistItems.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(WishlistItem item)
    {
        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync();
    }
}
