using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ShopeeClone.Application.Chat.Interfaces;

namespace ShopeeClone.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const string AdminsGroup = "admins";

    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }
        else
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var userName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            var conversation = await _chatService.GetOrCreateMyConversationAsync(userId, userName);
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversation.Id));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        if (await CanAccessAsync(conversationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
        }
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    internal static string GroupName(Guid conversationId) => $"conversation-{conversationId}";

    private async Task<bool> CanAccessAsync(Guid conversationId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = Context.User?.IsInRole("Admin") == true;

        var result = await _chatService.GetMessagesAsync(conversationId, userId, isAdmin, 1, 1);
        return result.Succeeded;
    }
}
