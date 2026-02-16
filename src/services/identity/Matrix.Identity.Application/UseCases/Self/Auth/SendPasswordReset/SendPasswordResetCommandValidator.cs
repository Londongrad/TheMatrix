using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Auth.SendPasswordReset
{
    public sealed class SendPasswordResetCommandValidator : AbstractValidator<SendPasswordResetCommand>
    {
        public SendPasswordResetCommandValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty()
               .WithMessage("Email is required.")
               .EmailAddress()
               .WithMessage("Email format is invalid.");
        }
    }
}
