using FluentValidation;
using HungStore.Application.FlashSales.Dtos;

namespace HungStore.Application.FlashSales.Validators;

public class AddFlashSaleItemRequestValidator : AbstractValidator<AddFlashSaleItemRequest>
{
    public AddFlashSaleItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SalePrice).GreaterThan(0);
        RuleFor(x => x.QuantityLimit).GreaterThan(0);
    }
}
