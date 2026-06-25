using ShopeeClone.Application.Common;
using ShopeeClone.Application.Vouchers.Dtos;

namespace ShopeeClone.Application.Vouchers.Interfaces;

public interface IVoucherService
{
    Task<ServiceResult<VoucherValidationResultDto>> ValidateAsync(string code, string userId, decimal orderSubtotal);
    Task RedeemAsync(Guid voucherId, string userId, Guid orderId);
    Task<PagedResult<VoucherDto>> GetPagedAsync(int page, int pageSize);
    Task<ServiceResult<VoucherDto>> CreateAsync(CreateVoucherRequest request);
    Task<ServiceResult<VoucherDto>> UpdateAsync(Guid id, UpdateVoucherRequest request);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
}
