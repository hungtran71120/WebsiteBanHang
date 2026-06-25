namespace ShopeeClone.Application.FlashSales.Dtos;

public class FlashSaleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; }
    public List<FlashSaleItemDto> Items { get; set; } = new();
}
