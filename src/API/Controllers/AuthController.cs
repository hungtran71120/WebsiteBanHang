using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using HungStore.Application.Auth.Dtos;
using HungStore.Application.Auth.Interfaces;

namespace HungStore.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(IAuthService authService, IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Succeeded)
        {
            return Unauthorized(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (!result.Succeeded)
        {
            return Unauthorized(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }
}
