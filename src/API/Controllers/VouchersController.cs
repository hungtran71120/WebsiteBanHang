using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopeeClone.Application.Cart.Interfaces;
using ShopeeClone.Application.Vouchers.Dtos;
using ShopeeClone.Application.Vouchers.Interfaces;

namespace ShopeeClone.API.Controllers;

[ApiController]
[Route("api/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;
    private readonly ICartService _cartService;
    private readonly IValidator<ValidateVoucherRequest> _validateValidator;
    private readonly IValidator<CreateVoucherRequest> _createValidator;
    private readonly IValidator<UpdateVoucherRequest> _updateValidator;

    public VouchersController(
        IVoucherService voucherService,
        ICartService cartService,
        IValidator<ValidateVoucherRequest> validateValidator,
        IValidator<CreateVoucherRequest> createValidator,
        IValidator<UpdateVoucherRequest> updateValidator)
    {
        _voucherService = voucherService;
        _cartService = cartService;
        _validateValidator = validateValidator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> Validate(ValidateVoucherRequest request)
    {
        var validation = await _validateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var cart = await _cartService.GetCartAsync(CurrentUserId);
        var result = await _voucherService.ValidateAsync(request.Code, CurrentUserId, cart.TotalAmount);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _voucherService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateVoucherRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _voucherService.CreateAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateVoucherRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _voucherService.UpdateAsync(id, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _voucherService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
