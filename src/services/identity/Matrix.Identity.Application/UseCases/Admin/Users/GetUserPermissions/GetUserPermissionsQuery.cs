using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions
{
    public sealed record GetUserPermissionsQuery(Guid UserId)
        : IRequest<IReadOnlyCollection<UserPermissionOverrideResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserPermissionsRead;
    }
}
