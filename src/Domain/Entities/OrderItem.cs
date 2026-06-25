using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? VariantDescription { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
