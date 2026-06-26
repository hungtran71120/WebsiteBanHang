using FluentValidation;
using HungStore.Application.FlashSales.Dtos;

namespace HungStore.Application.FlashSales.Validators;

public class CreateFlashSaleRequestValidator : AbstractValidator<CreateFlashSaleRequest>
{
    public CreateFlashSaleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt).WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu.");
    }
}
