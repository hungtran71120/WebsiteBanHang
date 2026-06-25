using FluentValidation;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.Application.Products.Validators;

public class CreateVariantOptionRequestValidator : AbstractValidator<CreateVariantOptionRequest>
{
    public CreateVariantOptionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Values).NotEmpty().WithMessage("Phải có ít nhất 1 giá trị.");
        RuleForEach(x => x.Values).ChildRules(value =>
        {
            value.RuleFor(v => v.Value).NotEmpty().MaximumLength(50);
        });
    }
}
