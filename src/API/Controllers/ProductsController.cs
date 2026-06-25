using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopeeClone.Application.Products.Dtos;
using ShopeeClone.Application.Products.Interfaces;
using ShopeeClone.Application.Recommendations.Interfaces;

namespace ShopeeClone.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private readonly IProductService _productService;
    private readonly IRecommendationService _recommendationService;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly IValidator<ProductFilterRequest> _filterValidator;
    private readonly IValidator<CreateProductVariantRequest> _createVariantValidator;
    private readonly IValidator<UpdateProductVariantRequest> _updateVariantValidator;
    private readonly IValidator<CreateVariantOptionRequest> _createVariantOptionValidator;
    private readonly IValidator<AddVariantOptionValueRequest> _addVariantOptionValueValidator;

    public ProductsController(
        IProductService productService,
        IRecommendationService recommendationService,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        IValidator<ProductFilterRequest> filterValidator,
        IValidator<CreateProductVariantRequest> createVariantValidator,
        IValidator<UpdateProductVariantRequest> updateVariantValidator,
        IValidator<CreateVariantOptionRequest> createVariantOptionValidator,
        IValidator<AddVariantOptionValueRequest> addVariantOptionValueValidator)
    {
        _productService = productService;
        _recommendationService = recommendationService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _createVariantValidator = createVariantValidator;
        _updateVariantValidator = updateVariantValidator;
        _createVariantOptionValidator = createVariantOptionValidator;
        _addVariantOptionValueValidator = addVariantOptionValueValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] ProductFilterRequest filter)
    {
        var validation = await _filterValidator.ValidateAsync(filter);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.GetPagedAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _productService.GetByIdAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}/related-products")]
    public async Task<IActionResult> GetRelatedProducts(Guid id)
    {
        var related = await _recommendationService.GetRelatedProductsAsync(id);
        return Ok(related);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.CreateAsync(request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.UpdateAsync(id, request);
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
        var result = await _productService.DeleteAsync(id);
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
        var result = await _productService.UpdateImageAsync(id, stream, file.ContentType);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/variant-options")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddVariantOption(Guid id, CreateVariantOptionRequest request)
    {
        var validation = await _createVariantOptionValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.AddVariantOptionAsync(id, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/variant-options/{optionId:guid}/values")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddVariantOptionValue(Guid id, Guid optionId, AddVariantOptionValueRequest request)
    {
        var validation = await _addVariantOptionValueValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.AddVariantOptionValueAsync(id, optionId, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/variant-options/{optionId:guid}/values/{valueId:guid}/image")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxImageSizeBytes)]
    public async Task<IActionResult> UploadVariantOptionValueImage(Guid id, Guid optionId, Guid valueId, IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest(new[] { "Vui lòng chọn ảnh để upload." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _productService.UpdateVariantOptionValueImageAsync(id, optionId, valueId, stream, file.ContentType);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}/variant-options/{optionId:guid}/values/{valueId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVariantOptionValue(Guid id, Guid optionId, Guid valueId)
    {
        var result = await _productService.DeleteVariantOptionValueAsync(id, optionId, valueId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}/variant-options/{optionId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVariantOption(Guid id, Guid optionId)
    {
        var result = await _productService.DeleteVariantOptionAsync(id, optionId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/variants")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddVariant(Guid id, CreateProductVariantRequest request)
    {
        var validation = await _createVariantValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.AddVariantAsync(id, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateVariant(Guid id, Guid variantId, UpdateProductVariantRequest request)
    {
        var validation = await _updateVariantValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _productService.UpdateVariantAsync(id, variantId, request);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId)
    {
        var result = await _productService.DeleteVariantAsync(id, variantId);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Data);
    }
}
