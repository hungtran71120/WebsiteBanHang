namespace HungStore.Application.Wishlist.Dtos;

public class WishlistItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}
