using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductIdPagedAsync(Guid productId, int page, int pageSize)
    {
        var query = _context.Reviews.AsNoTracking().Where(r => r.ProductId == productId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Review?> GetByIdAsync(Guid id)
    {
        return _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task<Review?> GetByProductAndUserAsync(Guid productId, string userId)
    {
        return _context.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
    }

    public async Task<(double AverageRating, int ReviewCount)> GetRatingSummaryAsync(Guid productId)
    {
        var query = _context.Reviews.AsNoTracking().Where(r => r.ProductId == productId);

        var reviewCount = await query.CountAsync();
        var averageRating = reviewCount == 0 ? 0 : await query.AverageAsync(r => (double)r.Rating);

        return (averageRating, reviewCount);
    }

    public async Task<IReadOnlyDictionary<Guid, (double AverageRating, int ReviewCount)>> GetRatingSummariesAsync(IReadOnlyCollection<Guid> productIds)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, (double AverageRating, int ReviewCount)>();
        }

        var summaries = await _context.Reviews
            .AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, AverageRating = g.Average(r => (double)r.Rating), ReviewCount = g.Count() })
            .ToListAsync();

        return summaries.ToDictionary(s => s.ProductId, s => (s.AverageRating, s.ReviewCount));
    }

    public async Task AddAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Review review)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Review review)
    {
        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
    }
}
