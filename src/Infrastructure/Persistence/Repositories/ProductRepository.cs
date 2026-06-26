using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    private static IQueryable<Product> IncludeVariants(IQueryable<Product> query)
    {
        return query
            .Include(p => p.VariantOptions).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValue1)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValue2);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        ProductSortBy sortBy,
        int page,
        int pageSize)
    {
        var query = IncludeVariants(_context.Products.AsNoTracking().Include(p => p.Category)).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p => p.Name.Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        var totalCount = await query.CountAsync();

        query = sortBy switch
        {
            ProductSortBy.Newest => query.OrderByDescending(p => p.CreatedAt),
            ProductSortBy.PriceAsc => query.OrderBy(p => p.Price),
            ProductSortBy.PriceDesc => query.OrderByDescending(p => p.Price),
            _ => query.OrderBy(p => p.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Product?> GetByIdAsync(Guid id)
    {
        return IncludeVariants(_context.Products.Include(p => p.Category)).FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    public Task<int> CountAsync()
    {
        return _context.Products.AsNoTracking().CountAsync();
    }

    public Task<ProductVariant?> GetVariantAsync(Guid productId, Guid variantId)
    {
        return _context.ProductVariants
            .Include(v => v.OptionValue1)
            .Include(v => v.OptionValue2)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
    }

    public async Task AddVariantAsync(ProductVariant variant)
    {
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateVariantAsync(ProductVariant variant)
    {
        _context.ProductVariants.Update(variant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteVariantAsync(ProductVariant variant)
    {
        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();
    }

    public Task<ProductVariantOption?> GetVariantOptionAsync(Guid productId, Guid optionId)
    {
        return _context.ProductVariantOptions
            .Include(o => o.Values)
            .FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId);
    }

    public async Task AddVariantOptionAsync(ProductVariantOption option)
    {
        _context.ProductVariantOptions.Add(option);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteVariantOptionAsync(ProductVariantOption option)
    {
        _context.ProductVariantOptions.Remove(option);
        await _context.SaveChangesAsync();
    }

    public Task<ProductVariantOptionValue?> GetVariantOptionValueAsync(Guid optionId, Guid valueId)
    {
        return _context.ProductVariantOptionValues
            .FirstOrDefaultAsync(v => v.Id == valueId && v.ProductVariantOptionId == optionId);
    }

    public async Task AddVariantOptionValueAsync(ProductVariantOptionValue value)
    {
        _context.ProductVariantOptionValues.Add(value);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateVariantOptionValueAsync(ProductVariantOptionValue value)
    {
        _context.ProductVariantOptionValues.Update(value);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteVariantOptionValueAsync(ProductVariantOptionValue value)
    {
        _context.ProductVariantOptionValues.Remove(value);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, Guid excludeProductId, int take)
    {
        return await IncludeVariants(_context.Products.AsNoTracking().Include(p => p.Category))
            .Where(p => p.CategoryId == categoryId && p.Id != excludeProductId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Product>> GetTopRatedAsync(IReadOnlyCollection<Guid> excludeIds, int take)
    {
        var ratingStats = await _context.Reviews
            .AsNoTracking()
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, AverageRating = g.Average(r => (double)r.Rating), ReviewCount = g.Count() })
            .ToListAsync();

        var rankedIds = ratingStats
            .Where(s => !excludeIds.Contains(s.ProductId))
            .OrderByDescending(s => s.AverageRating)
            .ThenByDescending(s => s.ReviewCount)
            .Select(s => s.ProductId)
            .Take(take)
            .ToList();

        if (rankedIds.Count < take)
        {
            var alreadyChosen = excludeIds.Concat(rankedIds).ToList();
            var fillerIds = await _context.Products
                .AsNoTracking()
                .Where(p => !alreadyChosen.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .Take(take - rankedIds.Count)
                .Select(p => p.Id)
                .ToListAsync();
            rankedIds.AddRange(fillerIds);
        }

        if (rankedIds.Count == 0)
        {
            return Array.Empty<Product>();
        }

        var products = await IncludeVariants(_context.Products.AsNoTracking().Include(p => p.Category))
            .Where(p => rankedIds.Contains(p.Id))
            .ToListAsync();

        return rankedIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string?>> GetImageUrlsAsync(IReadOnlyCollection<Guid> productIds)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, string?>();
        }

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.ImageUrl })
            .ToListAsync();

        return products.ToDictionary(p => p.Id, p => p.ImageUrl);
    }
}
