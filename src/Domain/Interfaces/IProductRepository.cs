using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Domain.Interfaces;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        ProductSortBy sortBy,
        int page,
        int pageSize);

    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task<int> CountAsync();

    Task<ProductVariant?> GetVariantAsync(Guid productId, Guid variantId);
    Task AddVariantAsync(ProductVariant variant);
    Task UpdateVariantAsync(ProductVariant variant);
    Task DeleteVariantAsync(ProductVariant variant);

    Task<ProductVariantOption?> GetVariantOptionAsync(Guid productId, Guid optionId);
    Task AddVariantOptionAsync(ProductVariantOption option);
    Task DeleteVariantOptionAsync(ProductVariantOption option);

    Task<ProductVariantOptionValue?> GetVariantOptionValueAsync(Guid optionId, Guid valueId);
    Task AddVariantOptionValueAsync(ProductVariantOptionValue value);
    Task UpdateVariantOptionValueAsync(ProductVariantOptionValue value);
    Task DeleteVariantOptionValueAsync(ProductVariantOptionValue value);

    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, Guid excludeProductId, int take);
    Task<IReadOnlyList<Product>> GetTopRatedAsync(IReadOnlyCollection<Guid> excludeIds, int take);
    Task<IReadOnlyDictionary<Guid, string?>> GetImageUrlsAsync(IReadOnlyCollection<Guid> productIds);
}
