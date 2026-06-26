using FluentValidation;
using HungStore.Application.Products.Dtos;

namespace HungStore.Application.Products.Validators;

public class UpdateProductVariantRequestValidator : AbstractValidator<UpdateProductVariantRequest>
{
    public UpdateProductVariantRequestValidator()
    {
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}
