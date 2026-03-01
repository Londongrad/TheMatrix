using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions
{
    public sealed record RevokeOtherMySessionsCommand : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeSessionsRevokeAll;
    }
}
