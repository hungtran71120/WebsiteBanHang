using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HungStore.Application.Cart.Dtos;
using HungStore.Application.Cart.Interfaces;

namespace HungStore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IValidator<AddCartItemRequest> _addValidator;
    private readonly IValidator<UpdateCartItemRequest> _updateValidator;

    public CartController(
        ICartService cartService,
        IValidator<AddCartItemRequest> addValidator,
        IValidator<UpdateCartItemRequest> updateValidator)
    {
        _cartService = cartService;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _cartService.GetCartAsync(CurrentUserId);
        return Ok(cart);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddCartItemRequest request)
    {
        var validation = await _addValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _cartService.AddItemAsync(CurrentUserId, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("items/{cartItemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid cartItemId, UpdateCartItemRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _cartService.UpdateItemAsync(CurrentUserId, cartItemId, request);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        var result = await _cartService.RemoveItemAsync(CurrentUserId, cartItemId);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
