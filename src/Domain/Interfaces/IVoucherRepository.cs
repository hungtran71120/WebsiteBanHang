using ShopeeClone.Domain.Entities;

namespace ShopeeClone.Domain.Interfaces;

public interface IVoucherRepository
{
    Task<Voucher?> GetByIdAsync(Guid id);
    Task<Voucher?> GetByCodeAsync(string code);
    Task<(IReadOnlyList<Voucher> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task AddAsync(Voucher voucher);
    Task UpdateAsync(Voucher voucher);
    Task DeleteAsync(Voucher voucher);
    Task<int> GetUserRedemptionCountAsync(Guid voucherId, string userId);
    Task AddRedemptionAsync(VoucherRedemption redemption);
}
