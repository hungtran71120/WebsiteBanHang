using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class Cart : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
}
