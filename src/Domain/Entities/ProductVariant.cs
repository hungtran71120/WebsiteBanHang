using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid OptionValue1Id { get; set; }
    public ProductVariantOptionValue? OptionValue1 { get; set; }
    public Guid? OptionValue2Id { get; set; }
    public ProductVariantOptionValue? OptionValue2 { get; set; }
    public int Stock { get; set; }
}
