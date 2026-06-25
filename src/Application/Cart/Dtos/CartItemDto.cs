namespace ShopeeClone.Application.Cart.Dtos;

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? VariantDescription { get; set; }
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
