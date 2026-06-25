using Microsoft.EntityFrameworkCore;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Infrastructure.Persistence.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Cart?> GetByUserIdAsync(string userId)
    {
        return _context.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .Include(c => c.Items).ThenInclude(ci => ci.ProductVariant).ThenInclude(v => v!.OptionValue1)
            .Include(c => c.Items).ThenInclude(ci => ci.ProductVariant).ThenInclude(v => v!.OptionValue2)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<Cart> CreateAsync(string userId)
    {
        var cart = new Cart { UserId = userId };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }

    public Task<CartItem?> GetItemAsync(Guid cartId, Guid productId, Guid? variantId)
    {
        return _context.CartItems
            .Include(ci => ci.Product)
            .Include(ci => ci.ProductVariant).ThenInclude(v => v!.OptionValue1)
            .Include(ci => ci.ProductVariant).ThenInclude(v => v!.OptionValue2)
            .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId && ci.ProductVariantId == variantId);
    }

    public async Task AddItemAsync(CartItem item)
    {
        _context.CartItems.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(CartItem item)
    {
        _context.CartItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(CartItem item)
    {
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task ClearAsync(Guid cartId)
    {
        var items = await _context.CartItems.Where(ci => ci.CartId == cartId).ToListAsync();
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}
