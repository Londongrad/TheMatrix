using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Account.SendEmailConfirmation
{
    public sealed class SendEmailConfirmationCommandValidator : AbstractValidator<SendEmailConfirmationCommand>
    {
        public SendEmailConfirmationCommandValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty()
               .WithMessage("Email is required")
               .EmailAddress()
               .WithMessage("Email has to look like email");
        }
    }
}
