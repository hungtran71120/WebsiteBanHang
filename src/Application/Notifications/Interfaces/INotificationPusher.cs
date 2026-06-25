using ShopeeClone.Application.Notifications.Dtos;

namespace ShopeeClone.Application.Notifications.Interfaces;

public interface INotificationPusher
{
    Task PushAsync(string userId, NotificationDto notification);
}
