using FluentAssertions;
using Moq;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;
using HungStore.Application.Users;
using HungStore.Application.Users.Dtos;

namespace HungStore.Application.UnitTests.Users;

public class UserServiceTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_identityServiceMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_WithUnknownUser_ReturnsFailure()
    {
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("missing")).ReturnsAsync((UserDto?)null);

        var result = await _sut.GetProfileAsync("missing");

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidUser_UpdatesAndReturnsUpdatedProfile()
    {
        const string userId = "user-1";
        var request = new UpdateProfileRequest { FullName = "Updated Name", PhoneNumber = "0911111111", Address = "456 New Street" };
        var updatedUser = new UserDto { Id = userId, Email = "user@test.com", FullName = request.FullName, PhoneNumber = request.PhoneNumber, Address = request.Address, Role = "Customer" };

        _identityServiceMock.Setup(x => x.UpdateProfileAsync(userId, request.FullName, request.PhoneNumber, request.Address)).ReturnsAsync(true);
        _identityServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(updatedUser);

        var result = await _sut.UpdateProfileAsync(userId, request);

        result.Succeeded.Should().BeTrue();
        result.Data!.FullName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithUnknownUser_ReturnsFailure()
    {
        const string userId = "missing";
        var request = new UpdateProfileRequest { FullName = "Name" };
        _identityServiceMock.Setup(x => x.UpdateProfileAsync(userId, request.FullName, request.PhoneNumber, request.Address)).ReturnsAsync(false);

        var result = await _sut.UpdateProfileAsync(userId, request);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsPagedResult()
    {
        var users = new List<UserDto> { new() { Id = "user-1", Email = "a@test.com" } };
        _identityServiceMock.Setup(x => x.GetAllUsersAsync(null, 1, 10)).ReturnsAsync((users, 1));

        var result = await _sut.GetAllUsersAsync(null, 1, 10);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Email.Should().Be("a@test.com");
    }

    [Fact]
    public async Task SetLockoutAsync_SelfLock_ReturnsFailure()
    {
        var result = await _sut.SetLockoutAsync("user-1", "user-1", true);

        result.Succeeded.Should().BeFalse();
        _identityServiceMock.Verify(x => x.SetLockoutAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task SetLockoutAsync_UnlockSelf_IsAllowed()
    {
        _identityServiceMock.Setup(x => x.SetLockoutAsync("user-1", false)).ReturnsAsync(true);
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-1")).ReturnsAsync(new UserDto { Id = "user-1" });

        var result = await _sut.SetLockoutAsync("user-1", "user-1", false);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SetLockoutAsync_OtherUser_LocksSuccessfully()
    {
        _identityServiceMock.Setup(x => x.SetLockoutAsync("user-2", true)).ReturnsAsync(true);
        _identityServiceMock.Setup(x => x.GetUserByIdAsync("user-2")).ReturnsAsync(new UserDto { Id = "user-2", IsLocked = true });

        var result = await _sut.SetLockoutAsync("admin-1", "user-2", true);

        result.Succeeded.Should().BeTrue();
        result.Data!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task SetLockoutAsync_UnknownUser_ReturnsFailure()
    {
        _identityServiceMock.Setup(x => x.SetLockoutAsync("missing", true)).ReturnsAsync(false);

        var result = await _sut.SetLockoutAsync("admin-1", "missing", true);

        result.Succeeded.Should().BeFalse();
    }
}
