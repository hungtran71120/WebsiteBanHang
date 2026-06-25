using ShopeeClone.Application.Auth.Dtos;
using ShopeeClone.Application.Common;

namespace ShopeeClone.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequest request);
    Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult<bool>> LogoutAsync(string refreshToken);
}
