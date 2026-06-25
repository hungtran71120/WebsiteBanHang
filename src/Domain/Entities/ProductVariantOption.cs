using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class ProductVariantOption : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<ProductVariantOptionValue> Values { get; set; } = new();
}
