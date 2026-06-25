using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class Wishlist : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public List<WishlistItem> Items { get; set; } = new();
}
