namespace ShopeeClone.Application.Vouchers.Dtos;

public class VoucherDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsedCount { get; set; }
    public int MaxUsagePerUser { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
