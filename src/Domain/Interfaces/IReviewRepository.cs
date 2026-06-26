using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface IReviewRepository
{
    Task<(IReadOnlyList<Review> Items, int TotalCount)> GetByProductIdPagedAsync(Guid productId, int page, int pageSize);
    Task<Review?> GetByIdAsync(Guid id);
    Task<Review?> GetByProductAndUserAsync(Guid productId, string userId);
    Task<(double AverageRating, int ReviewCount)> GetRatingSummaryAsync(Guid productId);
    Task<IReadOnlyDictionary<Guid, (double AverageRating, int ReviewCount)>> GetRatingSummariesAsync(IReadOnlyCollection<Guid> productIds);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(Review review);
}
