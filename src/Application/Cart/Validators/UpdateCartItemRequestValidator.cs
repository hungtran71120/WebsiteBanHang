using FluentValidation;
using ShopeeClone.Application.Cart.Dtos;

namespace ShopeeClone.Application.Cart.Validators;

public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
