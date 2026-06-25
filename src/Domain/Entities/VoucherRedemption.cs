using ShopeeClone.Domain.Common;

namespace ShopeeClone.Domain.Entities;

public class VoucherRedemption : BaseEntity
{
    public Guid VoucherId { get; set; }
    public Voucher? Voucher { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
}
