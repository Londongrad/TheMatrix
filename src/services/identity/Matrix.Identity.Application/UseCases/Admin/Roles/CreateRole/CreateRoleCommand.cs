using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole
{
    public sealed record CreateRoleCommand(string Name)
        : IRequest<RoleCreatedResult>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolesCreate;
    }
}
