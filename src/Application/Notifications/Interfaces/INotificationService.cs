using ShopeeClone.Application.Common;
using ShopeeClone.Application.Notifications.Dtos;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task NotifyOrderStatusChangedAsync(string userId, Guid orderId, OrderStatus newStatus);
    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(string userId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(string userId);
    Task<ServiceResult<bool>> MarkAsReadAsync(string userId, Guid notificationId);
    Task MarkAllAsReadAsync(string userId);
}
