using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}
