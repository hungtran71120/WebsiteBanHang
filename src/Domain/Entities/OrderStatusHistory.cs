using ShopeeClone.Domain.Common;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus Status { get; set; }
}
