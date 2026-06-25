using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class Conversation : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? LastMessagePreview { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public int UnreadCountForAdmin { get; set; }
    public int UnreadCountForCustomer { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
}
