using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HungStore.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        await base.OnConnectedAsync();
    }

    internal static string GroupName(string userId) => $"user-{userId}";
}
