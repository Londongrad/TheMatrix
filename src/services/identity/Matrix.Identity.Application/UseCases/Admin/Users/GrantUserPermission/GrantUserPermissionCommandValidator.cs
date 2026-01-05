using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission
{
    public sealed class GrantUserPermissionCommandValidator : AbstractValidator<GrantUserPermissionCommand>
    {
        public GrantUserPermissionCommandValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty()
               .WithMessage("UserId must not be empty");

            RuleFor(x => x.TargetPermissionKey)
               .NotEmpty()
               .WithMessage("Permission key must not be empty");
        }
    }
}
