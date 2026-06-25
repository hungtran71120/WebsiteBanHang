using ShopeeClone.Application.Common;
using ShopeeClone.Application.Reviews.Dtos;

namespace ShopeeClone.Application.Reviews.Interfaces;

public interface IReviewService
{
    Task<ServiceResult<ReviewDto>> CreateAsync(string userId, Guid productId, CreateReviewRequest request);
    Task<PagedResult<ReviewDto>> GetByProductIdAsync(Guid productId, int page, int pageSize);
    Task<ServiceResult<ReviewDto>> UpdateAsync(string userId, Guid reviewId, UpdateReviewRequest request);
    Task<ServiceResult<bool>> DeleteAsync(string userId, Guid reviewId);
}
