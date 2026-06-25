using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopeeClone.Application.Wishlist.Dtos;
using ShopeeClone.Application.Wishlist.Interfaces;

namespace ShopeeClone.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var wishlist = await _wishlistService.GetWishlistAsync(CurrentUserId);
        return Ok(wishlist);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddWishlistItemRequest request)
    {
        var result = await _wishlistService.AddAsync(CurrentUserId, request.ProductId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var result = await _wishlistService.RemoveAsync(CurrentUserId, productId);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
