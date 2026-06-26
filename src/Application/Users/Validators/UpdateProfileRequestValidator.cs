using FluentValidation;
using HungStore.Application.Users.Dtos;

namespace HungStore.Application.Users.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address is not null);
    }
}
