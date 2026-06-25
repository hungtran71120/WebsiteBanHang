using Microsoft.Extensions.Logging;
using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Application.Notifications.Dtos;
using ShopeeClone.Application.Notifications.Interfaces;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Enums;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Notifications;

public class NotificationService : INotificationService
{
    private static readonly IReadOnlyDictionary<OrderStatus, string> StatusLabels = new Dictionary<OrderStatus, string>
    {
        [OrderStatus.Pending] = "Chờ xác nhận",
        [OrderStatus.Confirmed] = "Đã xác nhận",
        [OrderStatus.Shipped] = "Đang giao",
        [OrderStatus.Delivered] = "Đã giao",
        [OrderStatus.Cancelled] = "Đã hủy"
    };

    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPusher _notificationPusher;
    private readonly IEmailSender _emailSender;
    private readonly IIdentityService _identityService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationPusher notificationPusher,
        IEmailSender emailSender,
        IIdentityService identityService,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _notificationPusher = notificationPusher;
        _emailSender = emailSender;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task NotifyOrderStatusChangedAsync(string userId, Guid orderId, OrderStatus newStatus)
    {
        var statusLabel = StatusLabels.TryGetValue(newStatus, out var label) ? label : newStatus.ToString();
        var notification = new Notification
        {
            UserId = userId,
            OrderId = orderId,
            Message = $"Đơn hàng #{orderId.ToString()[..8].ToUpperInvariant()} đã chuyển sang trạng thái \"{statusLabel}\"."
        };
        await _notificationRepository.AddAsync(notification);

        var dto = MapToDto(notification);
        await _notificationPusher.PushAsync(userId, dto);

        var user = await _identityService.GetUserByIdAsync(userId);
        if (user is not null)
        {
            try
            {
                await _emailSender.SendAsync(user.Email, "Cập nhật trạng thái đơn hàng", notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể gửi email thông báo đơn hàng {OrderId} tới {Email}.", orderId, user.Email);
            }
        }
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(string userId, int page, int pageSize)
    {
        var (items, totalCount) = await _notificationRepository.GetByUserIdPagedAsync(userId, page, pageSize);

        return new PagedResult<NotificationDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<int> GetUnreadCountAsync(string userId) => _notificationRepository.CountUnreadAsync(userId);

    public async Task<ServiceResult<bool>> MarkAsReadAsync(string userId, Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification is null || notification.UserId != userId)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy thông báo.");
        }

        notification.IsRead = true;
        await _notificationRepository.UpdateAsync(notification);

        return ServiceResult<bool>.Success(true);
    }

    public Task MarkAllAsReadAsync(string userId) => _notificationRepository.MarkAllAsReadAsync(userId);

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            OrderId = notification.OrderId,
            Message = notification.Message,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
