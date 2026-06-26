using FluentValidation;
using HungStore.Application.Vouchers.Dtos;
using HungStore.Domain.Enums;

namespace HungStore.Application.Vouchers.Validators;

public class UpdateVoucherRequestValidator : AbstractValidator<UpdateVoucherRequest>
{
    public UpdateVoucherRequestValidator()
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
    }
}
