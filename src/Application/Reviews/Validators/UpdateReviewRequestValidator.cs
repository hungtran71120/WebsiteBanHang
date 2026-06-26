using FluentValidation;
using HungStore.Application.Reviews.Dtos;

namespace HungStore.Application.Reviews.Validators;

public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Đánh giá phải từ 1 đến 5 sao.");
        RuleFor(x => x.Comment).NotEmpty().WithMessage("Vui lòng nhập nội dung đánh giá.").MaximumLength(1000);
    }
}
