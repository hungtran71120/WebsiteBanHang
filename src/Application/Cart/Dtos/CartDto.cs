namespace HungStore.Application.Cart.Dtos;

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
}
