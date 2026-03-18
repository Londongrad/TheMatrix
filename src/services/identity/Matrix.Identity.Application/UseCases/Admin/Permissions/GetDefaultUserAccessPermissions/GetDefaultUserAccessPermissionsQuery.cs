using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;
using AppPermissionKeys = Matrix.Identity.Application.Authorization.Permissions.PermissionKeys;

namespace Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions
{
    public sealed record GetDefaultUserAccessPermissionsQuery
        : IRequest<DefaultUserAccessPermissionsResult>, IRequirePermission
    {
        public string PermissionKey => AppPermissionKeys.IdentityRolePermissionsRead;
    }
}
