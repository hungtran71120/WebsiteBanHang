using HungStore.Application.Chat.Dtos;

namespace HungStore.Application.Chat.Interfaces;

public interface IChatNotifier
{
    Task NotifyMessageAsync(ChatMessageDto message);
    Task NotifyConversationUpdatedAsync(ConversationDto conversation);
}
