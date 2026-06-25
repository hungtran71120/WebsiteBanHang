using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public Guid WishlistId { get; set; }
    public Wishlist? Wishlist { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}
