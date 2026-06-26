using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public List<ProductVariant> Variants { get; set; } = new();
    public List<ProductVariantOption> VariantOptions { get; set; } = new();
}
