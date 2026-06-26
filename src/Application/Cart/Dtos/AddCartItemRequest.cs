namespace HungStore.Application.Cart.Dtos;

public class AddCartItemRequest
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public int Quantity { get; set; }
}
