using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class ProductVariantOptionValue : BaseEntity
{
    public Guid ProductVariantOptionId { get; set; }
    public ProductVariantOption? ProductVariantOption { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
