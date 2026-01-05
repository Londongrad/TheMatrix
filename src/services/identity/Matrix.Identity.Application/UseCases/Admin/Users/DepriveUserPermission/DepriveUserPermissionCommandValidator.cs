using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission
{
    public sealed class DepriveUserPermissionCommandValidator : AbstractValidator<DepriveUserPermissionCommand>
    {
        public DepriveUserPermissionCommandValidator()
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
