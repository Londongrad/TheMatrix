using FluentValidation;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName
{
    public sealed class ChangeDisplayNameCommandValidator : AbstractValidator<ChangeDisplayNameCommand>
    {
        public ChangeDisplayNameCommandValidator()
        {
            RuleFor(x => x.DisplayName)
               .MaximumLength(User.DisplayNameMaxLength)
               .WithMessage($"Display name must be at most {User.DisplayNameMaxLength} characters long.")
               .When(x => x.DisplayName is not null);
        }
    }
}
