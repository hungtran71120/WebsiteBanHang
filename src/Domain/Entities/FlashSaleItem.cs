using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class FlashSaleItem : BaseEntity
{
    public Guid FlashSaleId { get; set; }
    public FlashSale? FlashSale { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public decimal SalePrice { get; set; }
    public int QuantityLimit { get; set; }
    public int QuantitySold { get; set; }
}
