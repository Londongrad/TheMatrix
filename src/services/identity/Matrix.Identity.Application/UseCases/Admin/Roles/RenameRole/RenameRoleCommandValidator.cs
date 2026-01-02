using FluentValidation;
using Matrix.Identity.Domain.Entities;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole
{
    public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand>
    {
        public RenameRoleCommandValidator()
        {
            RuleFor(x => x.RoleId)
               .NotEmpty()
               .WithMessage("RoleId must not be empty");

            RuleFor(x => x.Name)
               .NotEmpty()
               .WithMessage("RoleId must not be empty")
               .MaximumLength(Role.NameMaxLength)
               .WithMessage($"Role name must be at most {Role.NameMaxLength} characters");
        }
    }
}
