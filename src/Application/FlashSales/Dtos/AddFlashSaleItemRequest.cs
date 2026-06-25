namespace ShopeeClone.Application.FlashSales.Dtos;

public class AddFlashSaleItemRequest
{
    public Guid ProductId { get; set; }
    public decimal SalePrice { get; set; }
    public int QuantityLimit { get; set; }
}
