using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed record UpdateUserRolesCommand(
        Guid UserId,
        IReadOnlyCollection<Guid> RoleIds)
        : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUserRolesUpdate;
    }
}
