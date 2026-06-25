using FluentValidation;
using ShopeeClone.Application.Vouchers.Dtos;
using ShopeeClone.Domain.Enums;

namespace ShopeeClone.Application.Vouchers.Validators;

public class CreateVoucherRequestValidator : AbstractValidator<CreateVoucherRequest>
{
    public CreateVoucherRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DiscountValue).GreaterThan(0);
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == VoucherDiscountType.Percentage)
            .WithMessage("Giảm theo % không được vượt quá 100.");
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxUsageCount).GreaterThan(0).When(x => x.MaxUsageCount.HasValue);
        RuleFor(x => x.MaxUsagePerUser).GreaterThan(0);
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow).WithMessage("Ngày hết hạn phải ở tương lai.");
    }
}
