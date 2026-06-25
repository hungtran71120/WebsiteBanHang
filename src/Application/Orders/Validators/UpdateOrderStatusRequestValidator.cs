using FluentValidation;
using ShopeeClone.Application.Orders.Dtos;

namespace ShopeeClone.Application.Orders.Validators;

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
