using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
    Task<bool> HasChildCategoriesAsync(Guid categoryId);
    Task<bool> HasProductsAsync(Guid categoryId);
}
