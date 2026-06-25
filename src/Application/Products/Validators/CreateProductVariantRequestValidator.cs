using FluentValidation;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.Application.Products.Validators;

public class CreateProductVariantRequestValidator : AbstractValidator<CreateProductVariantRequest>
{
    public CreateProductVariantRequestValidator()
    {
        RuleFor(x => x.OptionValue1Id).NotEqual(Guid.Empty);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}
