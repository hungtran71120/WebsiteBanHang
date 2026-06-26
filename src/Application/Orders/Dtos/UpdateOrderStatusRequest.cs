using HungStore.Domain.Enums;

namespace HungStore.Application.Orders.Dtos;

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
