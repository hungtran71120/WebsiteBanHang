using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdPagedAsync(string userId, int page, int pageSize);
    Task<int> CountUnreadAsync(string userId);
    Task<Notification?> GetByIdAsync(Guid id);
    Task UpdateAsync(Notification notification);
    Task MarkAllAsReadAsync(string userId);
}
