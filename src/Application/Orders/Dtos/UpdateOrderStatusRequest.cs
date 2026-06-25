using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Application.Orders.Dtos;

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
