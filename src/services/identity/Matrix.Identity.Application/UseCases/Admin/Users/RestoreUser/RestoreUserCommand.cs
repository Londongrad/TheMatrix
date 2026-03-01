using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Admin.Users.RestoreUser
{
    public sealed record RestoreUserCommand(Guid UserId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityUsersRestore;
    }
}
