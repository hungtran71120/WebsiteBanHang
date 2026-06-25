using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Products.Dtos;

public class ProductFilterRequest
{
    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public ProductSortBy SortBy { get; set; } = ProductSortBy.Default;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
