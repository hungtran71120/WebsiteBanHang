namespace ShopeeClone.Application.Products.Dtos;

public class CreateProductVariantRequest
{
    public Guid OptionValue1Id { get; set; }
    public Guid? OptionValue2Id { get; set; }
    public int Stock { get; set; }
}
