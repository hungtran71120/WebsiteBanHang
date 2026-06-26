namespace HungStore.Application.Categories.Dtos;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
}
