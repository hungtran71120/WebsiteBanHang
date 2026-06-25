namespace ShopeeClone.Application.Products.Dtos;

public class ProductVariantOptionValueDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
