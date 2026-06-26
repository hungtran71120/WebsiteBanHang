namespace HungStore.Application.FlashSales.Dtos;

public class FlashSaleItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int QuantityLimit { get; set; }
    public int QuantitySold { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public double SoldPercentage => QuantityLimit == 0 ? 0 : Math.Min(100, QuantitySold * 100.0 / QuantityLimit);
}
