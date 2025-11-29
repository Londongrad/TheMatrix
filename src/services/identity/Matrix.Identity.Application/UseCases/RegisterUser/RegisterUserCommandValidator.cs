using FluentValidation;

namespace Matrix.Identity.Application.UseCases.RegisterUser
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
                .MinimumLength(3).WithMessage("Username must be at least 3 characters long.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password)
                .WithMessage("Password and confirmation password do not match.");
        }
    }
}
