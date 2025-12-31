using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles
{
    public sealed record GetUserRolesQuery(Guid UserId)
        : IRequest<IReadOnlyCollection<UserRoleResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserRolesRead;
    }
}
