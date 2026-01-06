using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser
{
    public sealed record UnlockUserCommand(Guid UserId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersUnlock;
    }
}
