using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser
{
    public sealed class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
    {
        public UnlockUserCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("UserId must not be empty");
        }
    }
}
