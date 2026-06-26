using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public int Quantity { get; set; }
}
