using HungStore.Application.Common;
using HungStore.Application.Products.Dtos;

namespace HungStore.Application.Products.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductFilterRequest filter);
    Task<ServiceResult<ProductDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<ServiceResult<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<ProductDto>> UpdateImageAsync(Guid id, Stream content, string contentType);

    Task<ServiceResult<ProductDto>> AddVariantOptionAsync(Guid productId, CreateVariantOptionRequest request);
    Task<ServiceResult<ProductDto>> AddVariantOptionValueAsync(Guid productId, Guid optionId, AddVariantOptionValueRequest request);
    Task<ServiceResult<ProductDto>> DeleteVariantOptionValueAsync(Guid productId, Guid optionId, Guid valueId);
    Task<ServiceResult<ProductDto>> DeleteVariantOptionAsync(Guid productId, Guid optionId);
    Task<ServiceResult<ProductDto>> UpdateVariantOptionValueImageAsync(Guid productId, Guid optionId, Guid valueId, Stream content, string contentType);

    Task<ServiceResult<ProductDto>> AddVariantAsync(Guid productId, CreateProductVariantRequest request);
    Task<ServiceResult<ProductDto>> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantRequest request);
    Task<ServiceResult<ProductDto>> DeleteVariantAsync(Guid productId, Guid variantId);
}
