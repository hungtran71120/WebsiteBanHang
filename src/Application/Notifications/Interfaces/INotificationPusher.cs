using HungStore.Application.Notifications.Dtos;

namespace HungStore.Application.Notifications.Interfaces;

public interface INotificationPusher
{
    Task PushAsync(string userId, NotificationDto notification);
}
