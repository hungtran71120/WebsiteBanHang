using HungStore.Application.Auth.Dtos;
using HungStore.Domain.Entities;

namespace HungStore.Application.Auth.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(UserDto user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(string userId);
}
