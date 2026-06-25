using ShopeeClone.Application.Auth.Dtos;
using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Application.Auth.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(UserDto user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(string userId);
}
