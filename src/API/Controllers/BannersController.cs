using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HungStore.Application.Banners.Dtos;
using HungStore.Application.Banners.Interfaces;

namespace HungStore.API.Controllers;

[ApiController]
[Route("api/banners")]
public class BannersController : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private readonly IBannerService _bannerService;
    private readonly IValidator<CreateBannerRequest> _createValidator;
    private readonly IValidator<UpdateBannerRequest> _updateValidator;

    public BannersController(
        IBannerService bannerService,
        IValidator<CreateBannerRequest> createValidator,
        IValidator<UpdateBannerRequest> updateValidator)
    {
        _bannerService = bannerService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var banners = await _bannerService.GetActiveAsync();
        return Ok(banners);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var banners = await _bannerService.GetAllAsync();
        return Ok(banners);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _bannerService.GetByIdAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateBannerRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _bannerService.CreateAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateBannerRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _bannerService.UpdateAsync(id, request);
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
        var result = await _bannerService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxImageSizeBytes)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest(new[] { "Vui lòng chọn ảnh để upload." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _bannerService.UpdateImageAsync(id, stream, file.ContentType);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }
}
