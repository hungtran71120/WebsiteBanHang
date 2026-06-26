using HungStore.Application.Common;
using HungStore.Application.Vouchers.Dtos;
using HungStore.Application.Vouchers.Interfaces;
using HungStore.Domain.Entities;
using HungStore.Domain.Enums;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.Vouchers;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;

    public VoucherService(IVoucherRepository voucherRepository)
    {
        _voucherRepository = voucherRepository;
    }

    public async Task<ServiceResult<VoucherValidationResultDto>> ValidateAsync(string code, string userId, decimal orderSubtotal)
    {
        var voucher = await _voucherRepository.GetByCodeAsync(code);
        if (voucher is null)
        {
            return ServiceResult<VoucherValidationResultDto>.Failure("Mã giảm giá không hợp lệ.");
        }

        if (!voucher.IsActive)
        {
            return ServiceResult<VoucherValidationResultDto>.Failure("Mã giảm giá không còn hiệu lực.");
        }

        if (voucher.ExpiresAt < DateTime.UtcNow)
        {
            return ServiceResult<VoucherValidationResultDto>.Failure("Mã giảm giá đã hết hạn.");
        }

        if (orderSubtotal < voucher.MinOrderAmount)
        {
            return ServiceResult<VoucherValidationResultDto>.Failure(
                $"Đơn hàng tối thiểu ₫{voucher.MinOrderAmount:N0} để áp dụng mã này.");
        }

        if (voucher.MaxUsageCount.HasValue && voucher.UsedCount >= voucher.MaxUsageCount.Value)
        {
            return ServiceResult<VoucherValidationResultDto>.Failure("Mã giảm giá đã hết lượt sử dụng.");
        }

        var userRedemptionCount = await _voucherRepository.GetUserRedemptionCountAsync(voucher.Id, userId);
        if (userRedemptionCount >= voucher.MaxUsagePerUser)
        {
            return ServiceResult<VoucherValidationResultDto>.Failure("Bạn đã sử dụng hết số lần cho phép với mã này.");
        }

        var discount = voucher.DiscountType == VoucherDiscountType.Percentage
            ? orderSubtotal * voucher.DiscountValue / 100
            : voucher.DiscountValue;
        discount = Math.Min(discount, orderSubtotal);

        return ServiceResult<VoucherValidationResultDto>.Success(new VoucherValidationResultDto
        {
            VoucherId = voucher.Id,
            Code = voucher.Code,
            Subtotal = orderSubtotal,
            DiscountAmount = discount,
            FinalTotal = orderSubtotal - discount
        });
    }

    public async Task RedeemAsync(Guid voucherId, string userId, Guid orderId)
    {
        var voucher = await _voucherRepository.GetByIdAsync(voucherId);
        if (voucher is null)
        {
            return;
        }

        voucher.UsedCount += 1;
        await _voucherRepository.UpdateAsync(voucher);

        await _voucherRepository.AddRedemptionAsync(new VoucherRedemption
        {
            VoucherId = voucherId,
            UserId = userId,
            OrderId = orderId
        });
    }

    public async Task<PagedResult<VoucherDto>> GetPagedAsync(int page, int pageSize)
    {
        var (items, totalCount) = await _voucherRepository.GetPagedAsync(page, pageSize);

        return new PagedResult<VoucherDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ServiceResult<VoucherDto>> CreateAsync(CreateVoucherRequest request)
    {
        var existing = await _voucherRepository.GetByCodeAsync(request.Code);
        if (existing is not null)
        {
            return ServiceResult<VoucherDto>.Failure("Mã giảm giá này đã tồn tại.");
        }

        var voucher = new Voucher
        {
            Code = request.Code,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxUsageCount = request.MaxUsageCount,
            MaxUsagePerUser = request.MaxUsagePerUser,
            ExpiresAt = request.ExpiresAt,
            IsActive = request.IsActive
        };

        await _voucherRepository.AddAsync(voucher);

        return ServiceResult<VoucherDto>.Success(MapToDto(voucher));
    }

    public async Task<ServiceResult<VoucherDto>> UpdateAsync(Guid id, UpdateVoucherRequest request)
    {
        var voucher = await _voucherRepository.GetByIdAsync(id);
        if (voucher is null)
        {
            return ServiceResult<VoucherDto>.Failure("Không tìm thấy mã giảm giá.");
        }

        var existing = await _voucherRepository.GetByCodeAsync(request.Code);
        if (existing is not null && existing.Id != id)
        {
            return ServiceResult<VoucherDto>.Failure("Mã giảm giá này đã tồn tại.");
        }

        voucher.Code = request.Code;
        voucher.DiscountType = request.DiscountType;
        voucher.DiscountValue = request.DiscountValue;
        voucher.MinOrderAmount = request.MinOrderAmount;
        voucher.MaxUsageCount = request.MaxUsageCount;
        voucher.MaxUsagePerUser = request.MaxUsagePerUser;
        voucher.ExpiresAt = request.ExpiresAt;
        voucher.IsActive = request.IsActive;

        await _voucherRepository.UpdateAsync(voucher);

        return ServiceResult<VoucherDto>.Success(MapToDto(voucher));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        var voucher = await _voucherRepository.GetByIdAsync(id);
        if (voucher is null)
        {
            return ServiceResult<bool>.Failure("Không tìm thấy mã giảm giá.");
        }

        await _voucherRepository.DeleteAsync(voucher);

        return ServiceResult<bool>.Success(true);
    }

    private static VoucherDto MapToDto(Voucher voucher)
    {
        return new VoucherDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            DiscountType = voucher.DiscountType.ToString(),
            DiscountValue = voucher.DiscountValue,
            MinOrderAmount = voucher.MinOrderAmount,
            MaxUsageCount = voucher.MaxUsageCount,
            UsedCount = voucher.UsedCount,
            MaxUsagePerUser = voucher.MaxUsagePerUser,
            ExpiresAt = voucher.ExpiresAt,
            IsActive = voucher.IsActive
        };
    }
}
