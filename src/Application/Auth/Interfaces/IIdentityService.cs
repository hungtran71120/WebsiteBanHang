using HungStore.Application.Auth.Dtos;

namespace HungStore.Application.Auth.Interfaces;

public interface IIdentityService
{
    Task<IdentityOperationResult> RegisterAsync(string email, string password, string fullName, string? phoneNumber, string? address);
    Task<bool> CheckPasswordAsync(string email, string password);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<IReadOnlyList<string>> GetRolesAsync(string userId);
    Task AssignRoleAsync(string userId, string role);
    Task<bool> UpdateProfileAsync(string userId, string fullName, string? phoneNumber, string? address);
    Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetAllUsersAsync(string? keyword, int page, int pageSize);
    Task<bool> SetLockoutAsync(string userId, bool locked);
    Task<int> CountUsersAsync();
}
