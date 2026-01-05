using FluentValidation;
using Matrix.Identity.Domain.Rules;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangePassword
{
    public sealed class ChangePasswordCommandValidator
        : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.CurrentPassword)
               .NotEmpty()
               .WithMessage("Current password is required.");

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
