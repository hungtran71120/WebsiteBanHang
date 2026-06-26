using FluentValidation;
using HungStore.Application.Orders.Dtos;

namespace HungStore.Application.Orders.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
