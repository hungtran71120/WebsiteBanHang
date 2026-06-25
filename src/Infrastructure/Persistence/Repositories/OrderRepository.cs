using Microsoft.EntityFrameworkCore;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Enums;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetByUserIdPagedAsync(string userId, OrderStatus? status, int page, int pageSize)
    {
        var query = _context.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllPagedAsync(OrderStatus? status, int page, int pageSize)
    {
        var query = _context.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Order?> GetByIdAsync(Guid id)
    {
        return _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
    }

    public Task<bool> HasDeliveredOrderForProductAsync(string userId, Guid productId)
    {
        return _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
            .SelectMany(o => o.Items)
            .AnyAsync(i => i.ProductId == productId);
    }

    public Task<decimal> GetTotalRevenueAsync()
    {
        return _context.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount);
    }

    public async Task<IReadOnlyDictionary<OrderStatus, int>> GetOrderCountsByStatusAsync()
    {
        var counts = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    public async Task<IReadOnlyList<Guid>> GetPurchasedProductIdsAsync(string userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .SelectMany(o => o.Items)
            .Select(i => i.ProductId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetPurchasedCategoryIdsAsync(string userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .SelectMany(o => o.Items)
            .Join(_context.Products.AsNoTracking(), i => i.ProductId, p => p.Id, (i, p) => p.CategoryId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSoldCountsAsync(IReadOnlyCollection<Guid> productIds)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var counts = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .Where(i => productIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(i => i.Quantity) })
            .ToListAsync();

        return counts.ToDictionary(x => x.ProductId, x => x.Total);
    }

    public async Task AddStatusHistoryAsync(OrderStatusHistory history)
    {
        _context.OrderStatusHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId)
    {
        return await _context.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetOrderIdsEligibleForAutoDeliveryAsync(OrderStatus fromStatus, DateTime cutoff)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status == fromStatus)
            .Where(o => _context.OrderStatusHistories
                .Any(h => h.OrderId == o.Id && h.Status == fromStatus && h.CreatedAt <= cutoff))
            .Select(o => o.Id)
            .ToListAsync();
    }
}
