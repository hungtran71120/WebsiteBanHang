using FluentValidation;
using HungStore.Application.Products.Dtos;

namespace HungStore.Application.Products.Validators;

public class AddVariantOptionValueRequestValidator : AbstractValidator<AddVariantOptionValueRequest>
{
    public AddVariantOptionValueRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(50);
    }
}
