using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopeeClone.Application.Users.Dtos;
using ShopeeClone.Application.Users.Interfaces;

namespace ShopeeClone.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;

    public UsersController(IUserService userService, IValidator<UpdateProfileRequest> updateProfileValidator)
    {
        _userService = userService;
        _updateProfileValidator = updateProfileValidator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _userService.GetProfileAsync(CurrentUserId);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var validation = await _updateProfileValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _userService.UpdateProfileAsync(CurrentUserId, request);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _userService.GetAllUsersAsync(keyword, page, pageSize);
        return Ok(result);
    }

    [HttpPut("{id}/lockout")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetLockout(string id, SetUserLockoutRequest request)
    {
        var result = await _userService.SetLockoutAsync(CurrentUserId, id, request.Locked);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
