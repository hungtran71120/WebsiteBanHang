using FluentValidation;
using HungStore.Application.Auth.Dtos;

namespace HungStore.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address is not null);
    }
}
