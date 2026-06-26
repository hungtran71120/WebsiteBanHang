using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public bool IsFromAdmin { get; set; }
    public string Content { get; set; } = string.Empty;
}
