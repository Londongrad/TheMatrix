using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions
{
    public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
    {
        public UpdateRolePermissionsCommandValidator()
        {
            RuleFor(x => x.RoleId)
               .NotEmpty()
               .WithMessage("RoleId must not be empty");
        }
    }
}
