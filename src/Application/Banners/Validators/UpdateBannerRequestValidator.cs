using FluentValidation;
using HungStore.Application.Banners.Dtos;

namespace HungStore.Application.Banners.Validators;

public class UpdateBannerRequestValidator : AbstractValidator<UpdateBannerRequest>
{
    public UpdateBannerRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subtitle).MaximumLength(300);
        RuleFor(x => x.LinkUrl).NotEmpty().MaximumLength(300).Must(x => x.StartsWith('/'))
            .WithMessage("Link phải là đường dẫn nội bộ, bắt đầu bằng '/'.");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
