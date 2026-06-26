using FluentAssertions;
using Moq;
using HungStore.Application.Auth;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_identityServiceMock.Object, _tokenServiceMock.Object, _refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_AssignsCustomerRoleAndReturnsTokens()
    {
        const string userId = "user-1";
        var request = new RegisterRequest { Email = "new@test.com", Password = "Password123", FullName = "New User" };

        _identityServiceMock.Setup(x => x.GetUserByEmailAsync(request.Email)).ReturnsAsync((UserDto?)null);
        _identityServiceMock.Setup(x => x.RegisterAsync(request.Email, request.Password, request.FullName, request.PhoneNumber, request.Address))
            .ReturnsAsync(IdentityOperationResult.Success(userId));
        SetupIssueTokens(userId, request.Email, "Customer");

        var result = await _sut.RegisterAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        _identityServiceMock.Verify(x => x.AssignRoleAsync(userId, "Customer"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        var request = new RegisterRequest { Email = "existing@test.com", Password = "Password123", FullName = "Existing User" };
        _identityServiceMock.Setup(x => x.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(new UserDto { Id = "user-1", Email = request.Email, FullName = request.FullName, Role = "Customer" });

        var result = await _sut.RegisterAsync(request);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email đã được sử dụng.");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        var request = new LoginRequest { Email = "user@test.com", Password = "WrongPassword" };
        _identityServiceMock.Setup(x => x.CheckPasswordAsync(request.Email, request.Password)).ReturnsAsync(false);

        var result = await _sut.LoginAsync(request);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithUnknownToken_ReturnsFailure()
    {
        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("missing")).ReturnsAsync((RefreshToken?)null);

        var result = await _sut.RefreshTokenAsync("missing");

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_RevokesOldTokenAndIssuesNewOne()
    {
        const string userId = "user-1";
        var stored = new RefreshToken { Token = "old-token", UserId = userId, ExpiresAt = DateTime.UtcNow.AddDays(1) };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync("old-token")).ReturnsAsync(stored);
        SetupIssueTokens(userId, "user@test.com", "Customer");

        var result = await _sut.RefreshTokenAsync("old-token");

        result.Succeeded.Should().BeTrue();
        stored.RevokedAt.Should().NotBeNull();
        stored.ReplacedByToken.Should().Be("refresh-token");
        _refreshTokenRepositoryMock.Verify(x => x.UpdateAsync(stored), Times.Once);
    }

    private void SetupIssueTokens(string userId, string email, string role)
    {
        var user = new UserDto { Id = userId, Email = email, FullName = "Test User", Role = role };
        _identityServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(user);
        _identityServiceMock.Setup(x => x.GetRolesAsync(userId)).ReturnsAsync(new List<string> { role });
        _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>()))
            .Returns(("access-token", DateTime.UtcNow.AddHours(1)));
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken(userId))
            .Returns(new RefreshToken { Token = "refresh-token", UserId = userId, ExpiresAt = DateTime.UtcNow.AddDays(7) });
        _refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _refreshTokenRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
    }
}
