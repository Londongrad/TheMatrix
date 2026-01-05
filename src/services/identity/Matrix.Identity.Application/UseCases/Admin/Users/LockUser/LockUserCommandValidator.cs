using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.LockUser
{
    public sealed class LockUserCommandValidator : AbstractValidator<LockUserCommand>
    {
        public LockUserCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("UserId must not be empty");
        }
    }
}
