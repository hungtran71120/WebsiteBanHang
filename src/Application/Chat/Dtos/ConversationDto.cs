namespace HungStore.Application.Chat.Dtos;

public class ConversationDto
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? LastMessagePreview { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCountForAdmin { get; set; }
    public int UnreadCountForCustomer { get; set; }
}
