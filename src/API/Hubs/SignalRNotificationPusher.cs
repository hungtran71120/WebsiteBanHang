using Microsoft.AspNetCore.SignalR;
using HungStore.Application.Notifications.Dtos;
using HungStore.Application.Notifications.Interfaces;

namespace HungStore.API.Hubs;

public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushAsync(string userId, NotificationDto notification)
    {
        return _hubContext.Clients.Group(NotificationHub.GroupName(userId)).SendAsync("ReceiveNotification", notification);
    }
}
