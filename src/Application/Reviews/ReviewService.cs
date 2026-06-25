using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Application.Reviews.Dtos;
using ShopeeClone.Application.Reviews.Interfaces;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Reviews;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;

    public ReviewService(
        IReviewRepository reviewRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IIdentityService identityService)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _identityService = identityService;
    }

    public async Task<ServiceResult<ReviewDto>> CreateAsync(string userId, Guid productId, CreateReviewRequest request)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return ServiceResult<ReviewDto>.Failure("Không tìm thấy sản phẩm.");
        }

        var hasPurchased = await _orderRepository.HasDeliveredOrderForProductAsync(userId, productId);
        if (!hasPurchased)
        {
            return ServiceResult<ReviewDto>.Failure("Bạn cần mua và nhận sản phẩm này trước khi đánh giá.");
        }

        var existingReview = await _reviewRepository.GetByProductAndUserAsync(productId, userId);
        if (existingReview is not null)
        {
            return ServiceResult<ReviewDto>.Failure("Bạn đã đánh giá sản phẩm này rồi.");
        }

        var user = await _identityService.GetUserByIdAsync(userId);

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            UserName = user?.FullName ?? "Người dùng",
            Rating = request.Rating,
            Comment = request.Comment
        };

        await _reviewRepository.AddAsync(review);

        return ServiceResult<ReviewDto>.Success(MapToDto(review));
    }

    public async Task<PagedResult<ReviewDto>> GetByProductIdAsync(Guid productId, int page, int pageSize)
    {
        var (items, totalCount) = await _reviewRepository.GetByProductIdPagedAsync(productId, page, pageSize);

        return new PagedResult<ReviewDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<ReviewDto>> UpdateAsync(string userId, Guid reviewId, UpdateReviewRequest request)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review is null || review.UserId != userId)
        {
            return ServiceResult<ReviewDto>.Failure("Không tìm thấy đánh giá.");
        }

        review.Rating = request.Rating;
        review.Comment = request.Comment;

        await _reviewRepository.UpdateAsync(review);

        return ServiceResult<ReviewDto>.Success(MapToDto(review));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(string userId, Guid reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review is null || review.UserId != userId)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy đánh giá.");
        }

        await _reviewRepository.DeleteAsync(review);

        return ServiceResult<bool>.Success(true);
    }

    private static ReviewDto MapToDto(Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = review.UserName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}
