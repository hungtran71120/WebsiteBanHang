using FluentValidation;
using ShopeeClone.Application.Vouchers.Dtos;

namespace ShopeeClone.Application.Vouchers.Validators;

public class ValidateVoucherRequestValidator : AbstractValidator<ValidateVoucherRequest>
{
    public ValidateVoucherRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
    }
}
