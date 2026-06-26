using FluentValidation;
using HungStore.Application.Chat.Dtos;

namespace HungStore.Application.Chat.Validators;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().WithMessage("Vui lòng nhập nội dung tin nhắn.").MaximumLength(2000);
    }
}
