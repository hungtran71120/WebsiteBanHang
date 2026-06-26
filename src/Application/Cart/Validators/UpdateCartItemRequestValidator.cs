using FluentValidation;
using HungStore.Application.Cart.Dtos;

namespace HungStore.Application.Cart.Validators;

public class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
