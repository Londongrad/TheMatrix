using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions
{
    public sealed class GetRolePermissionsQueryValidator : AbstractValidator<GetRolePermissionsQuery>
    {
        public GetRolePermissionsQueryValidator()
        {
            RuleFor(x => x.RoleId)
               .NotEmpty();
        }
    }
}
