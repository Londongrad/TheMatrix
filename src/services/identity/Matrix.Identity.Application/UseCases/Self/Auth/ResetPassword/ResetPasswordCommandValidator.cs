using FluentValidation;
using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Application.UseCases.Self.Auth.ResetPassword
{
    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty();

            RuleFor(x => x.Token)
               .NotEmpty();

            RuleFor(x => x.NewPassword)
               .NotEmpty()
               .WithMessage("New password is required.")
               .MinimumLength(PasswordRules.MinLength)
               .WithMessage($"New password must be at least {PasswordRules.MinLength} characters long.")
               .MaximumLength(PasswordRules.MaxLength)
               .WithMessage($"New password must be at most {PasswordRules.MaxLength} characters long.")
               .Matches("[a-z]")
               .WithMessage("New password must contain at least one lowercase letter.")
               .Matches("[A-Z]")
               .WithMessage("New password must contain at least one uppercase letter.")
               .Matches("[0-9]")
               .WithMessage("New password must contain at least one digit.")
               .Matches("[^a-zA-Z0-9]")
               .WithMessage("New password must contain at least one special character.");
        }
    }
}
