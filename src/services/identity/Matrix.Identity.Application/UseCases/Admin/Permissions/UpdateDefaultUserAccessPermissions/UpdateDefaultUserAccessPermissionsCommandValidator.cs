using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions
{
    public sealed class UpdateDefaultUserAccessPermissionsCommandValidator
        : AbstractValidator<UpdateDefaultUserAccessPermissionsCommand>
    {
        public UpdateDefaultUserAccessPermissionsCommandValidator()
        {
            RuleFor(x => x.PermissionKeys)
               .NotNull();
        }
    }
}
