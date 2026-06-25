using ShopeeClone.Application.Auth.Dtos;
using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Auth;

public class AuthService : IAuthService
{
    private const string CustomerRole = "Customer";

    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(IIdentityService identityService, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterRequest request)
    {
        var existing = await _identityService.GetUserByEmailAsync(request.Email);
        if (existing is not null)
        {
            return ServiceResult<AuthResponseDto>.Failure("Email đã được sử dụng.");
        }

        var result = await _identityService.RegisterAsync(request.Email, request.Password, request.FullName, request.PhoneNumber, request.Address);
        if (!result.Succeeded)
        {
            return ServiceResult<AuthResponseDto>.Failure(result.Errors);
        }

        await _identityService.AssignRoleAsync(result.UserId!, CustomerRole);

        return await IssueTokensAsync(result.UserId!);
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequest request)
    {
        var user = await _identityService.GetUserByEmailAsync(request.Email);
        if (user is not null && user.IsLocked)
        {
            return ServiceResult<AuthResponseDto>.Failure("Tài khoản của bạn đã bị khóa.");
        }

        var isValid = await _identityService.CheckPasswordAsync(request.Email, request.Password);
        if (!isValid)
        {
            return ServiceResult<AuthResponseDto>.Failure("Email hoặc mật khẩu không đúng.");
        }

        return await IssueTokensAsync(user!.Id);
    }

    public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (stored is null || !stored.IsActive)
        {
            return ServiceResult<AuthResponseDto>.Failure("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        stored.RevokedAt = DateTime.UtcNow;

        var result = await IssueTokensAsync(stored.UserId);

        stored.ReplacedByToken = result.Data?.RefreshToken;
        await _refreshTokenRepository.UpdateAsync(stored);

        return result;
    }

    public async Task<ServiceResult<bool>> LogoutAsync(string refreshToken)
    {
        var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (stored is null || !stored.IsActive)
        {
            return ServiceResult<bool>.Failure("Refresh token không hợp lệ.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(stored);

        return ServiceResult<bool>.Success(true);
    }

    private async Task<ServiceResult<AuthResponseDto>> IssueTokensAsync(string userId)
    {
        var user = await _identityService.GetUserByIdAsync(userId);
        var roles = await _identityService.GetRolesAsync(userId);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user!, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(userId);

        await _refreshTokenRepository.AddAsync(refreshToken);

        return ServiceResult<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = expiresAt,
            User = user!
        });
    }
}
