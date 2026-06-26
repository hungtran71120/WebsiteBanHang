using HungStore.Application.Categories.Dtos;
using HungStore.Application.Common;

namespace HungStore.Application.Categories.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<ServiceResult<CategoryDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<CategoryDto>> CreateAsync(CreateCategoryRequest request);
    Task<ServiceResult<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
