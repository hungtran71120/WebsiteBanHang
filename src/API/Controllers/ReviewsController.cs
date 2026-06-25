using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopeeClone.Application.Reviews.Dtos;
using ShopeeClone.Application.Reviews.Interfaces;

namespace ShopeeClone.API.Controllers;

[ApiController]
[Route("api")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly IValidator<CreateReviewRequest> _createValidator;
    private readonly IValidator<UpdateReviewRequest> _updateValidator;

    public ReviewsController(
        IReviewService reviewService,
        IValidator<CreateReviewRequest> createValidator,
        IValidator<UpdateReviewRequest> updateValidator)
    {
        _reviewService = reviewService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("products/{productId:guid}/reviews")]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetByProductIdAsync(productId, page, pageSize);
        return Ok(result);
    }

    [HttpPost("products/{productId:guid}/reviews")]
    [Authorize]
    public async Task<IActionResult> Create(Guid productId, CreateReviewRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _reviewService.CreateAsync(CurrentUserId, productId, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, UpdateReviewRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _reviewService.UpdateAsync(CurrentUserId, id, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _reviewService.DeleteAsync(CurrentUserId, id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
