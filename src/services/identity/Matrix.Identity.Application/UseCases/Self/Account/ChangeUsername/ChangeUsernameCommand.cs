using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername
{
    public sealed record ChangeUsernameCommand(string Username) : IRequest<string>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeUsernameChange;
    }
}
