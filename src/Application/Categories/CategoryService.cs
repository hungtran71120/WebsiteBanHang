using ShopeeClone.Application.Categories.Dtos;
using ShopeeClone.Application.Categories.Interfaces;
using ShopeeClone.Application.Common;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Application.Categories;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        var lookup = categories.ToDictionary(c => c.Id);

        return categories.Select(c => MapToDto(c, lookup)).ToList();
    }

    public async Task<ServiceResult<CategoryDto>> GetByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.Failure("Không tìm thấy danh mục.");
        }

        return ServiceResult<CategoryDto>.Success(await MapWithParentNameAsync(category));
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(CreateCategoryRequest request)
    {
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value);
            if (parent is null)
            {
                return ServiceResult<CategoryDto>.Failure("Danh mục cha không tồn tại.");
            }
        }

        var category = new Category
        {
            Name = request.Name,
            ParentCategoryId = request.ParentCategoryId
        };

        await _categoryRepository.AddAsync(category);

        return ServiceResult<CategoryDto>.Success(await MapWithParentNameAsync(category));
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.Failure("Không tìm thấy danh mục.");
        }

        if (request.ParentCategoryId == id)
        {
            return ServiceResult<CategoryDto>.Failure("Danh mục không thể là cha của chính nó.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value);
            if (parent is null)
            {
                return ServiceResult<CategoryDto>.Failure("Danh mục cha không tồn tại.");
            }
        }

        category.Name = request.Name;
        category.ParentCategoryId = request.ParentCategoryId;

        await _categoryRepository.UpdateAsync(category);

        return ServiceResult<CategoryDto>.Success(await MapWithParentNameAsync(category));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy danh mục.");
        }

        if (await _categoryRepository.HasChildCategoriesAsync(id))
        {
            return ServiceResult<bool>.Failure("Không thể xóa danh mục còn danh mục con.");
        }

        if (await _categoryRepository.HasProductsAsync(id))
        {
            return ServiceResult<bool>.Failure("Không thể xóa danh mục còn sản phẩm.");
        }

        await _categoryRepository.DeleteAsync(category);

        return ServiceResult<bool>.Success(true);
    }

    private async Task<CategoryDto> MapWithParentNameAsync(Category category)
    {
        Category? parent = category.ParentCategoryId.HasValue
            ? await _categoryRepository.GetByIdAsync(category.ParentCategoryId.Value)
            : null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = parent?.Name
        };
    }

    private static CategoryDto MapToDto(Category category, IReadOnlyDictionary<Guid, Category> lookup)
    {
        string? parentName = category.ParentCategoryId.HasValue && lookup.TryGetValue(category.ParentCategoryId.Value, out var parent)
            ? parent.Name
            : null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = parentName
        };
    }
}
