using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface IBannerRepository
{
    Task<IReadOnlyList<Banner>> GetAllAsync();
    Task<IReadOnlyList<Banner>> GetActiveAsync();
    Task<Banner?> GetByIdAsync(Guid id);
    Task AddAsync(Banner banner);
    Task UpdateAsync(Banner banner);
    Task DeleteAsync(Banner banner);
}
