using FluentValidation;
using HungStore.Application.Categories.Dtos;

namespace HungStore.Application.Categories.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
