using HungStore.Domain.Common;
using HungStore.Domain.Enums;

namespace HungStore.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public OrderStatus Status { get; set; }
}
