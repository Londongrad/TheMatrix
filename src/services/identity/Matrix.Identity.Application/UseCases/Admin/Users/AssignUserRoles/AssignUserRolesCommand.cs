using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.AssignUserRoles
{
    public sealed record AssignUserRolesCommand(Guid UserId, IReadOnlyCollection<Guid> RoleIds)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserRolesAssign;
    }
}
