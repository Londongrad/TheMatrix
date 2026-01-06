using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession
{
    public sealed record RevokeMySessionCommand(Guid SessionId) : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeSessionsRevoke;
    }
}
