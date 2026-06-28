using HungStore.Domain.Entities;

namespace HungStore.Domain.Interfaces;

public interface IVoucherRepository
{
    Task<Voucher?> GetByIdAsync(Guid id);
    Task<Voucher?> GetByCodeAsync(string code);
    Task<(IReadOnlyList<Voucher> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task<IReadOnlyList<Voucher>> GetActiveAsync();
    Task AddAsync(Voucher voucher);
    Task UpdateAsync(Voucher voucher);
    Task DeleteAsync(Voucher voucher);
    Task<int> GetUserRedemptionCountAsync(Guid voucherId, string userId);
    Task AddRedemptionAsync(VoucherRedemption redemption);
}
