namespace ShopeeClone.Application.Products.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int SoldCount { get; set; }
    public decimal? FlashSalePrice { get; set; }
    public int? FlashSaleQuantityRemaining { get; set; }
    public DateTime? FlashSaleEndsAt { get; set; }
    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductVariantOptionDto> VariantOptions { get; set; } = new();
}
