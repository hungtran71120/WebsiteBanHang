using FluentValidation;
using HungStore.Application.Orders.Dtos;

namespace HungStore.Application.Orders.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PaymentMethod).IsInEnum();
    }
}
