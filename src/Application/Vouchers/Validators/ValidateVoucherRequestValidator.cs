using FluentValidation;
using HungStore.Application.Vouchers.Dtos;

namespace HungStore.Application.Vouchers.Validators;

public class ValidateVoucherRequestValidator : AbstractValidator<ValidateVoucherRequest>
{
    public ValidateVoucherRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
    }
}
