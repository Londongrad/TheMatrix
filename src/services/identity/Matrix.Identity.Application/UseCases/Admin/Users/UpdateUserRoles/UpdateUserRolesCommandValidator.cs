using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
    {
        public UpdateUserRolesCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("UserId must not be empty");
        }
    }
}
