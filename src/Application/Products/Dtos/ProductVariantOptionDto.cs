namespace ShopeeClone.Application.Products.Dtos;

public class ProductVariantOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<ProductVariantOptionValueDto> Values { get; set; } = new();
}
