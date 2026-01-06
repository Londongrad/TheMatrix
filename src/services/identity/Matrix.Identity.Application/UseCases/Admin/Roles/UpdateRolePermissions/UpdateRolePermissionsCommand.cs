using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions
{
    public sealed record UpdateRolePermissionsCommand(
        Guid RoleId,
        IReadOnlyCollection<string> RolePermissionKeys)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityRolePermissionsUpdate;
    }
}
