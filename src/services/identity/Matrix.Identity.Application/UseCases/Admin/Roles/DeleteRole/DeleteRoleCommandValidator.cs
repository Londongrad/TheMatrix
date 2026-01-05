using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole
{
    public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
    {
        public DeleteRoleCommandValidator()
        {
            RuleFor(x => x.RoleId)
               .NotEmpty()
               .WithMessage("RoleId must not be empty.");
        }
    }
}
