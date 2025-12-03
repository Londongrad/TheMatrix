using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Auth.RegisterUser
{
    public sealed class RegisterUserCommandValidator
    : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
                .MaximumLength(15).WithMessage("Username must be at most 15 characters long.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("New password is required.")
                .MinimumLength(6)
                .WithMessage("New password must be at least 6 characters long.")
                .Matches("[a-z]")
                .WithMessage("New password must contain at least one lowercase letter.")
                .Matches("[A-Z]")
                .WithMessage("New password must contain at least one uppercase letter.")
                .Matches("[0-9]")
                .WithMessage("New password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]")
                .WithMessage("New password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password)
                .WithMessage("Password and confirmation password do not match.");
        }
    }
}
