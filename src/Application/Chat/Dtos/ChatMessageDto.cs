namespace HungStore.Application.Chat.Dtos;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool IsFromAdmin { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
