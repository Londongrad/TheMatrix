using FluentValidation;
using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername
{
    public sealed class ChangeUsernameCommandValidator : AbstractValidator<ChangeUsernameCommand>
    {
        public ChangeUsernameCommandValidator()
        {
            RuleFor(x => x.Username)
               .NotEmpty()
               .WithMessage("Username is required.")
               .MinimumLength(Username.MinLength)
               .WithMessage($"Username must be at least {Username.MinLength} characters long.")
               .MaximumLength(Username.MaxLength)
               .WithMessage($"Username must be at most {Username.MaxLength} characters long.");
        }
    }
}
