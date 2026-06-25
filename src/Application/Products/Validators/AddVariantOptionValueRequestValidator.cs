using FluentValidation;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.Application.Products.Validators;

public class AddVariantOptionValueRequestValidator : AbstractValidator<AddVariantOptionValueRequest>
{
    public AddVariantOptionValueRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().MaximumLength(50);
    }
}
