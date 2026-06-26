using Microsoft.EntityFrameworkCore;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Infrastructure.Persistence.Repositories;

public class VoucherRepository : IVoucherRepository
{
    private readonly AppDbContext _context;

    public VoucherRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Voucher?> GetByIdAsync(Guid id)
    {
        return _context.Vouchers.FirstOrDefaultAsync(v => v.Id == id);
    }

    public Task<Voucher?> GetByCodeAsync(string code)
    {
        return _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
    }

    public async Task<(IReadOnlyList<Voucher> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Vouchers.AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Voucher voucher)
    {
        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Voucher voucher)
    {
        _context.Vouchers.Update(voucher);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Voucher voucher)
    {
        _context.Vouchers.Remove(voucher);
        await _context.SaveChangesAsync();
    }

    public Task<int> GetUserRedemptionCountAsync(Guid voucherId, string userId)
    {
        return _context.VoucherRedemptions
            .AsNoTracking()
            .CountAsync(r => r.VoucherId == voucherId && r.UserId == userId);
    }

    public async Task AddRedemptionAsync(VoucherRedemption redemption)
    {
        _context.VoucherRedemptions.Add(redemption);
        await _context.SaveChangesAsync();
    }
}
