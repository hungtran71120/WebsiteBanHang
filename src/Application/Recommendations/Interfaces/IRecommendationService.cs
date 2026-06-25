using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.Application.Recommendations.Interfaces;

public interface IRecommendationService
{
    Task<IReadOnlyList<ProductDto>> GetRelatedProductsAsync(Guid productId, int take = 10);
    Task<IReadOnlyList<ProductDto>> GetPersonalizedRecommendationsAsync(string? userId, int take = 10);
}
