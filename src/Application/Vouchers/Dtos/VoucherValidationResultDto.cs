namespace HungStore.Application.Vouchers.Dtos;

public class VoucherValidationResultDto
{
    public Guid VoucherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
}
