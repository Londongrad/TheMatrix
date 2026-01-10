using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole
{
    public sealed record DeleteRoleCommand(Guid RoleId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolesDelete;
    }
}
