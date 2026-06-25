using FluentValidation;
using ShopeeClone.Application.FlashSales.Dtos;

namespace ShopeeClone.Application.FlashSales.Validators;

public class UpdateFlashSaleItemRequestValidator : AbstractValidator<UpdateFlashSaleItemRequest>
{
    public UpdateFlashSaleItemRequestValidator()
    {
        RuleFor(x => x.SalePrice).GreaterThan(0);
        RuleFor(x => x.QuantityLimit).GreaterThan(0);
    }
}
