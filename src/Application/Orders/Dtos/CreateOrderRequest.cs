using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Application.Orders.Dtos;

public class CreateOrderRequest
{
    public string ShippingAddress { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string? VoucherCode { get; set; }
}
