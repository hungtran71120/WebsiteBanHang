using Microsoft.EntityFrameworkCore;
using ShopeeClone.Domain.Entities;
using ShopeeClone.Domain.Interfaces;

namespace ShopeeClone.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    public Task<Category?> GetByIdAsync(Guid id)
    {
        return _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasChildCategoriesAsync(Guid categoryId)
    {
        return _context.Categories.AnyAsync(c => c.ParentCategoryId == categoryId);
    }

    public Task<bool> HasProductsAsync(Guid categoryId)
    {
        return _context.Products.AnyAsync(p => p.CategoryId == categoryId);
    }
}
