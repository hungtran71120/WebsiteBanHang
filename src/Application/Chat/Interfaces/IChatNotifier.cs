using ShopeeClone.Application.Chat.Dtos;

namespace ShopeeClone.Application.Chat.Interfaces;

public interface IChatNotifier
{
    Task NotifyMessageAsync(ChatMessageDto message);
    Task NotifyConversationUpdatedAsync(ConversationDto conversation);
}
