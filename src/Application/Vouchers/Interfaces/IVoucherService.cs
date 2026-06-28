using HungStore.Application.Common;
using HungStore.Application.Vouchers.Dtos;

namespace HungStore.Application.Vouchers.Interfaces;

public interface IVoucherService
{
    Task<ServiceResult<VoucherValidationResultDto>> ValidateAsync(string code, string userId, decimal orderSubtotal);
    Task RedeemAsync(Guid voucherId, string userId, Guid orderId);
    Task<PagedResult<VoucherDto>> GetPagedAsync(int page, int pageSize);
    Task<IReadOnlyList<VoucherDto>> GetAvailableForUserAsync(string userId);
    Task<ServiceResult<VoucherDto>> CreateAsync(CreateVoucherRequest request);
    Task<ServiceResult<VoucherDto>> UpdateAsync(Guid id, UpdateVoucherRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
