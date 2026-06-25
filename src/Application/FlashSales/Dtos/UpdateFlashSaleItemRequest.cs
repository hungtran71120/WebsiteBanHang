namespace ShopeeClone.Application.FlashSales.Dtos;

public class UpdateFlashSaleItemRequest
{
    public decimal SalePrice { get; set; }
    public int QuantityLimit { get; set; }
}
