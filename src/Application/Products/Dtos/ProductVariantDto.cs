namespace HungStore.Application.Products.Dtos;

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public Guid OptionValue1Id { get; set; }
    public string OptionValue1Text { get; set; } = string.Empty;
    public Guid? OptionValue2Id { get; set; }
    public string? OptionValue2Text { get; set; }
    public int Stock { get; set; }
}
