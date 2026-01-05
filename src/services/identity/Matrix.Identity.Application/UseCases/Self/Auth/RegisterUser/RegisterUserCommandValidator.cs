using FluentValidation;
using Matrix.Identity.Domain.Rules;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser
{
    public sealed class RegisterUserCommandValidator
        : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty()
               .WithMessage("Email is required.")
               .EmailAddress()
               .WithMessage("Email format is invalid.");

            RuleFor(x => x.Username)
               .NotEmpty()
               .WithMessage("Username is required.")
               .MinimumLength(Username.MinLength)
               .WithMessage($"Username must be at least {Username.MinLength} characters long.")
               .MaximumLength(Username.MaxLength)
               .WithMessage($"Username must be at most {Username.MaxLength} characters long.");

            RuleFor(x => x.Password)
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
