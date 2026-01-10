using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions
{
    public sealed record GetUserPermissionsQuery(Guid UserId)
        : IRequest<IReadOnlyCollection<UserPermissionOverrideResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserPermissionsRead;
    }
}
