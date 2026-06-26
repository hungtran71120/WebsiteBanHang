using FluentValidation;
using HungStore.Application.Products.Dtos;

namespace HungStore.Application.Products.Validators;

public class ProductFilterRequestValidator : AbstractValidator<ProductFilterRequest>
{
    public ProductFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);
    }
}
