namespace HungStore.Application.Products.Dtos;

public class CreateVariantOptionRequest
{
    public string Name { get; set; } = string.Empty;
    public List<CreateVariantOptionValueRequest> Values { get; set; } = new();
}
