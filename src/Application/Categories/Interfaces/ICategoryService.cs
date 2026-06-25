using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.Common;

namespace ShopeeClone.Application.Categories.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<ServiceResult<CategoryDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CategoryDto>> CreateAsync(CreateCategoryRequest request);
    Task<ServiceResult<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
