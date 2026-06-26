using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }
}
