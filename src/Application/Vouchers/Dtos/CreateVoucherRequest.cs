using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Application.Vouchers.Dtos;

public class CreateVoucherRequest
{
    public string Code { get; set; } = string.Empty;
    public VoucherDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int MaxUsagePerUser { get; set; } = 1;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
