using HungStore.Application.Products;
using HungStore.Application.Products.Dtos;
using HungStore.Application.Recommendations.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.Recommendations;

public class RecommendationService : IRecommendationService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IReviewRepository _reviewRepository;

    public RecommendationService(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IReviewRepository reviewRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> GetRelatedProductsAsync(Guid productId, int take = 10)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return Array.Empty<ProductDto>();
        }

        var related = await _productRepository.GetByCategoryAsync(product.CategoryId, productId, take);
        return await MapToDtosAsync(related);
    }

    public async Task<IReadOnlyList<ProductDto>> GetPersonalizedRecommendationsAsync(string? userId, int take = 10)
    {
        IReadOnlyList<Guid> excludeIds = Array.Empty<Guid>();
        IReadOnlyList<Guid> purchasedCategoryIds = Array.Empty<Guid>();

        if (!string.IsNullOrEmpty(userId))
        {
            excludeIds = await _orderRepository.GetPurchasedProductIdsAsync(userId);
            purchasedCategoryIds = await _orderRepository.GetPurchasedCategoryIdsAsync(userId);
        }

        if (purchasedCategoryIds.Count > 0)
        {
            var candidates = await _productRepository.GetTopRatedAsync(excludeIds, take * 3);
            var inPurchasedCategories = candidates.Where(p => purchasedCategoryIds.Contains(p.CategoryId)).Take(take).ToList();
            if (inPurchasedCategories.Count > 0)
            {
                return await MapToDtosAsync(inPurchasedCategories);
            }
        }

        var fallback = await _productRepository.GetTopRatedAsync(excludeIds, take);
        return await MapToDtosAsync(fallback);
    }

    private async Task<IReadOnlyList<ProductDto>> MapToDtosAsync(IEnumerable<Product> products)
    {
        var dtos = new List<ProductDto>();
        foreach (var product in products)
        {
            var dto = ProductService.MapToDto(product);
            (dto.AverageRating, dto.ReviewCount) = await _reviewRepository.GetRatingSummaryAsync(product.Id);
            dtos.Add(dto);
        }

        return dtos;
    }
}
