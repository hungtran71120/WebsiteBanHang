using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class BannerRepository : IBannerRepository
{
    private readonly AppDbContext _context;

    public BannerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Banner>> GetAllAsync()
    {
        return await _context.Banners.AsNoTracking().OrderBy(b => b.DisplayOrder).ToListAsync();
    }

    public async Task<IReadOnlyList<Banner>> GetActiveAsync()
    {
        return await _context.Banners.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.DisplayOrder).ToListAsync();
    }

    public Task<Banner?> GetByIdAsync(Guid id)
    {
        return _context.Banners.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task AddAsync(Banner banner)
    {
        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Banner banner)
    {
        _context.Banners.Update(banner);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Banner banner)
    {
        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync();
    }
}
