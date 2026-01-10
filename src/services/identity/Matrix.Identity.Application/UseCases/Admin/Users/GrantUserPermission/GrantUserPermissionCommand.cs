using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission
{
    public sealed record GrantUserPermissionCommand(
        Guid UserId,
        string TargetPermissionKey)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserPermissionsGrant;
    }
}
