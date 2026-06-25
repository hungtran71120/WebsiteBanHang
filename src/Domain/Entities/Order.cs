using ShopeeClone.Domain.Common;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Domain.Entities;

public class Order : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public bool IsPaid { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? VoucherId { get; set; }
    public string? VoucherCode { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
