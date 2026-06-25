using Microsoft.EntityFrameworkCore;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Infrastructure.Persistence.Repositories;

public class FlashSaleRepository : IFlashSaleRepository
{
    private readonly AppDbContext _context;

    public FlashSaleRepository(AppDbContext context)
    {
        _context = context;
    }

    private static IQueryable<FlashSale> IncludeItems(IQueryable<FlashSale> query)
    {
        return query.Include(f => f.Items).ThenInclude(i => i.Product);
    }

    public Task<FlashSale?> GetByIdAsync(Guid id)
    {
        return IncludeItems(_context.FlashSales).FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<(IReadOnlyList<FlashSale> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = IncludeItems(_context.FlashSales.AsNoTracking());

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<FlashSale?> GetActiveAsync(DateTime now)
    {
        return IncludeItems(_context.FlashSales.AsNoTracking())
            .Where(f => f.IsActive && f.StartsAt <= now && f.EndsAt >= now)
            .OrderBy(f => f.StartsAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyDictionary<Guid, FlashSaleItem>> GetActiveItemsForProductsAsync(IReadOnlyCollection<Guid> productIds, DateTime now)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, FlashSaleItem>();
        }

        var items = await _context.FlashSaleItems
            .AsNoTracking()
            .Include(i => i.FlashSale)
            .Where(i => productIds.Contains(i.ProductId)
                && i.FlashSale!.IsActive
                && i.FlashSale.StartsAt <= now
                && i.FlashSale.EndsAt >= now)
            .ToListAsync();

        return items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task AddAsync(FlashSale flashSale)
    {
        _context.FlashSales.Add(flashSale);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FlashSale flashSale)
    {
        _context.FlashSales.Update(flashSale);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(FlashSale flashSale)
    {
        _context.FlashSales.Remove(flashSale);
        await _context.SaveChangesAsync();
    }

    public Task<FlashSaleItem?> GetItemAsync(Guid flashSaleId, Guid itemId)
    {
        return _context.FlashSaleItems
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.FlashSaleId == flashSaleId);
    }

    public async Task AddItemAsync(FlashSaleItem item)
    {
        _context.FlashSaleItems.Add(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemAsync(FlashSaleItem item)
    {
        _context.FlashSaleItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(FlashSaleItem item)
    {
        _context.FlashSaleItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> TryIncrementQuantitySoldAsync(Guid flashSaleItemId, int quantity)
    {
        var affected = await _context.FlashSaleItems
            .Where(i => i.Id == flashSaleItemId && i.QuantitySold + quantity <= i.QuantityLimit)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.QuantitySold, i => i.QuantitySold + quantity));

        return affected > 0;
    }
}
