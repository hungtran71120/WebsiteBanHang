using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class Wishlist : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public List<WishlistItem> Items { get; set; } = new();
}
