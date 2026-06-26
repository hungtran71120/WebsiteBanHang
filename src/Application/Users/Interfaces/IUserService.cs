using HungStore.Application.Auth.Dtos;
using HungStore.Application.Common;
using HungStore.Application.Users.Dtos;

namespace HungStore.Application.Users.Interfaces;

public interface IUserService
{
    Task<ServiceResult<UserDto>> GetProfileAsync(string userId);
    Task<ServiceResult<UserDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<PagedResult<UserDto>> GetAllUsersAsync(string? keyword, int page, int pageSize);
    Task<ServiceResult<UserDto>> SetLockoutAsync(string currentUserId, string targetUserId, bool locked);
}
