using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.Identity.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions
{
    public sealed record UpdateDefaultUserAccessPermissionsCommand(IReadOnlyCollection<string> PermissionKeys)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => AppPermissionKeys.IdentityRolePermissionsUpdate;
    }
}
