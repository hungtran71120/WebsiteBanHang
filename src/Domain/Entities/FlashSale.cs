using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class FlashSale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<FlashSaleItem> Items { get; set; } = new();
}
