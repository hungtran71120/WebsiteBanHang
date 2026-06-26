using HungStore.Application.Common;
using HungStore.Application.Reviews.Dtos;

namespace HungStore.Application.Reviews.Interfaces;

public interface IReviewService
{
    Task<ServiceResult<ReviewDto>> CreateAsync(string userId, Guid productId, CreateReviewRequest request);
    Task<PagedResult<ReviewDto>> GetByProductIdAsync(Guid productId, int page, int pageSize);
    Task<ServiceResult<ReviewDto>> UpdateAsync(string userId, Guid reviewId, UpdateReviewRequest request);
    Task<ServiceResult<bool>> DeleteAsync(string userId, Guid reviewId);
}
