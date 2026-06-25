using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class Cart : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
}
