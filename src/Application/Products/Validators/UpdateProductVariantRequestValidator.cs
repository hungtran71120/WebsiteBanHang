using FluentValidation;
using ShopeeClone.Application.Products.Dtos;

namespace ShopeeClone.Application.Products.Validators;

public class UpdateProductVariantRequestValidator : AbstractValidator<UpdateProductVariantRequest>
{
    public UpdateProductVariantRequestValidator()
    {
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}
