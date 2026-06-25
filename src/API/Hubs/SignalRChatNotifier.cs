using Microsoft.AspNetCore.SignalR;
using ShopeeClone.Application.Chat.Dtos;
using ShopeeClone.Application.Chat.Interfaces;

namespace ShopeeClone.API.Hubs;

public class SignalRChatNotifier : IChatNotifier
{
    private const string AdminsGroup = "admins";

    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMessageAsync(ChatMessageDto message)
    {
        return _hubContext.Clients.Group(ChatHub.GroupName(message.ConversationId)).SendAsync("ReceiveMessage", message);
    }

    public Task NotifyConversationUpdatedAsync(ConversationDto conversation)
    {
        return Task.WhenAll(
            _hubContext.Clients.Group(AdminsGroup).SendAsync("ConversationUpdated", conversation),
            _hubContext.Clients.Group(ChatHub.GroupName(conversation.Id)).SendAsync("ConversationUpdated", conversation));
    }
}
