using HungStore.Application.Auth.Dtos;
using HungStore.Application.Common;

namespace HungStore.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequest request);
    Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult<bool>> LogoutAsync(string refreshToken);
}
