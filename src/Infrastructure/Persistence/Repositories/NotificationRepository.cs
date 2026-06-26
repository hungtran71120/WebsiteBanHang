using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetByUserIdPagedAsync(string userId, int page, int pageSize)
    {
        var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<int> CountUnreadAsync(string userId)
    {
        return _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public Task<Notification?> GetByIdAsync(Guid id)
    {
        return _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
    }
}
