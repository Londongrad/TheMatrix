using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser
{
    public sealed record UnlockUserCommand(Guid UserId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersUnlock;
    }
}
