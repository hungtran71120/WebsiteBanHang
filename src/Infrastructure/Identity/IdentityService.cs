using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;

namespace HungStore.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IdentityOperationResult> RegisterAsync(string email, string password, string fullName, string? phoneNumber, string? address)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Address = address
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return IdentityOperationResult.Failure(result.Errors.Select(e => e.Description));
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : await MapToDtoAsync(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : await MapToDtoAsync(user);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task AssignRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return;
        }

        await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<bool> UpdateProfileAsync(string userId, string fullName, string? phoneNumber, string? address)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.Address = address;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<(IReadOnlyList<UserDto> Items, int TotalCount)> GetAllUsersAsync(string? keyword, int page, int pageSize)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u => u.Email!.Contains(keyword) || u.FullName.Contains(keyword));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserDto>();
        foreach (var user in users)
        {
            items.Add(await MapToDtoAsync(user));
        }

        return (items, totalCount);
    }

    public async Task<bool> SetLockoutAsync(string userId, bool locked)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);

        return true;
    }

    public Task<int> CountUsersAsync()
    {
        return _userManager.Users.CountAsync();
    }

    private async Task<UserDto> MapToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = roles.FirstOrDefault() ?? "Customer",
            IsLocked = await _userManager.IsLockedOutAsync(user)
        };
    }
}
