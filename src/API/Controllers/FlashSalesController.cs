using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HungStore.Application.FlashSales.Dtos;
using HungStore.Application.FlashSales.Interfaces;

namespace HungStore.API.Controllers;

[ApiController]
[Route("api/flash-sales")]
public class FlashSalesController : ControllerBase
{
    private readonly IFlashSaleService _flashSaleService;
    private readonly IValidator<CreateFlashSaleRequest> _createValidator;
    private readonly IValidator<AddFlashSaleItemRequest> _addItemValidator;
    private readonly IValidator<UpdateFlashSaleItemRequest> _updateItemValidator;

    public FlashSalesController(
        IFlashSaleService flashSaleService,
        IValidator<CreateFlashSaleRequest> createValidator,
        IValidator<AddFlashSaleItemRequest> addItemValidator,
        IValidator<UpdateFlashSaleItemRequest> updateItemValidator)
    {
        _flashSaleService = flashSaleService;
        _createValidator = createValidator;
        _addItemValidator = addItemValidator;
        _updateItemValidator = updateItemValidator;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var flashSale = await _flashSaleService.GetActiveFlashSaleAsync();
        return Ok(flashSale);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _flashSaleService.GetPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _flashSaleService.GetByIdAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateFlashSaleRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _flashSaleService.CreateAsync(request);
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
        var result = await _flashSaleService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddItem(Guid id, AddFlashSaleItemRequest request)
    {
        var validation = await _addItemValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _flashSaleService.AddItemAsync(id, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, UpdateFlashSaleItemRequest request)
    {
        var validation = await _updateItemValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _flashSaleService.UpdateItemAsync(id, itemId, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId)
    {
        var result = await _flashSaleService.DeleteItemAsync(id, itemId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }
}
