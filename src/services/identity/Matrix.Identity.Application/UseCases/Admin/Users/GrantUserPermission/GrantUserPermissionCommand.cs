using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission
{
    public sealed record GrantUserPermissionCommand(Guid UserId, string PermissionKey)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserPermissionsGrant;
    }
}
