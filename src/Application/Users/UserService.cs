using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;
using HungStore.Application.Common;
using HungStore.Application.Users.Dtos;
using HungStore.Application.Users.Interfaces;

namespace HungStore.Application.Users;

public class UserService : IUserService
{
    private readonly IIdentityService _identityService;

    public UserService(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<ServiceResult<UserDto>> GetProfileAsync(string userId)
    {
        var user = await _identityService.GetUserByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult<UserDto>.Failure("Không tìm thấy người dùng.");
        }

        return ServiceResult<UserDto>.Success(user);
    }

    public async Task<ServiceResult<UserDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var updated = await _identityService.UpdateProfileAsync(userId, request.FullName, request.PhoneNumber, request.Address);
        if (!updated)
        {
            return ServiceResult<UserDto>.Failure("Không tìm thấy người dùng.");
        }

        var user = await _identityService.GetUserByIdAsync(userId);
        return ServiceResult<UserDto>.Success(user!);
    }

    public async Task<PagedResult<UserDto>> GetAllUsersAsync(string? keyword, int page, int pageSize)
    {
        var (items, totalCount) = await _identityService.GetAllUsersAsync(keyword, page, pageSize);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<UserDto>> SetLockoutAsync(string currentUserId, string targetUserId, bool locked)
    {
        if (locked && currentUserId == targetUserId)
        {
            return ServiceResult<UserDto>.Failure("Không thể tự khóa tài khoản của chính mình.");
        }

        var updated = await _identityService.SetLockoutAsync(targetUserId, locked);
        if (!updated)
        {
            return ServiceResult<UserDto>.Failure("Không tìm thấy người dùng.");
        }

        var user = await _identityService.GetUserByIdAsync(targetUserId);
        return ServiceResult<UserDto>.Success(user!);
    }
}
