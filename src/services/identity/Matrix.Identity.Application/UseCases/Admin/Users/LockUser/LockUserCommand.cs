using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.LockUser
{
    public sealed record LockUserCommand(Guid UserId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersLock;
    }
}
