namespace ShopeeClone.Application.Categories.Dtos;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
}
