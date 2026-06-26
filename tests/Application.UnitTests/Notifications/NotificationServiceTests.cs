using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;
using HungStore.Application.Notifications;
using HungStore.Application.Notifications.Dtos;
using HungStore.Application.Notifications.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Enums;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Notifications;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
    private readonly Mock<INotificationPusher> _notificationPusherMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(
            _notificationRepositoryMock.Object,
            _notificationPusherMock.Object,
            _emailSenderMock.Object,
            _identityServiceMock.Object,
            new Mock<ILogger<NotificationService>>().Object);
    }

    [Fact]
    public async Task NotifyOrderStatusChangedAsync_SavesNotification_PushesRealtime_AndSendsEmail()
    {
        var orderId = Guid.NewGuid();
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-1"))
            .ReturnsAsync(new UserDto { Id = "user-1", Email = "user1@test.com" });

        await _sut.NotifyOrderStatusChangedAsync("user-1", orderId, OrderStatus.Confirmed);

        _notificationRepositoryMock.Verify(x => x.AddAsync(It.Is<Notification>(n =>
            n.UserId == "user-1" && n.OrderId == orderId && n.Message.Contains("Đã xác nhận"))), Times.Once);
        _notificationPusherMock.Verify(x => x.PushAsync("user-1", It.Is<NotificationDto>(n => n.OrderId == orderId)), Times.Once);
        _emailSenderMock.Verify(x => x.SendAsync("user1@test.com", It.IsAny<string>(), It.Is<string>(b => b.Contains("Đã xác nhận"))), Times.Once);
    }

    [Fact]
    public async Task NotifyOrderStatusChangedAsync_UserNotFound_DoesNotSendEmail()
    {
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-1")).ReturnsAsync((UserDto?)null);

        await _sut.NotifyOrderStatusChangedAsync("user-1", Guid.NewGuid(), OrderStatus.Shipped);

        _emailSenderMock.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifyOrderStatusChangedAsync_EmailSendingThrows_DoesNotPropagate()
    {
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-1"))
            .ReturnsAsync(new UserDto { Id = "user-1", Email = "user1@test.com" });
        _emailSenderMock.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var act = async () => await _sut.NotifyOrderStatusChangedAsync("user-1", Guid.NewGuid(), OrderStatus.Delivered);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetUnreadCountAsync_DelegatesToRepository()
    {
        _notificationRepositoryMock.Setup(x => x.CountUnreadAsync("user-1")).ReturnsAsync(5);

        var result = await _sut.GetUnreadCountAsync("user-1");

        result.Should().Be(5);
    }

    [Fact]
    public async Task MarkAsReadAsync_NotificationBelongsToAnotherUser_ReturnsFailure()
    {
        var notification = new Notification { UserId = "owner" };
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notification.Id)).ReturnsAsync(notification);

        var result = await _sut.MarkAsReadAsync("someone-else", notification.Id);

        result.Succeeded.Should().BeFalse();
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_Owner_SetsIsReadTrue()
    {
        var notification = new Notification { UserId = "user-1", IsRead = false };
        _notificationRepositoryMock.Setup(x => x.GetByIdAsync(notification.Id)).ReturnsAsync(notification);

        var result = await _sut.MarkAsReadAsync("user-1", notification.Id);

        result.Succeeded.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(notification), Times.Once);
    }
}
