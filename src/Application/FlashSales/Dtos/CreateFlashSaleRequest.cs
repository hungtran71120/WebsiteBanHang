namespace HungStore.Application.FlashSales.Dtos;

public class CreateFlashSaleRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}
