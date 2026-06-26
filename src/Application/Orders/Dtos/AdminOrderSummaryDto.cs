namespace HungStore.Application.Orders.Dtos;

public class AdminOrderSummaryDto
{
    public Guid Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
