using Matrix.BuildingBlocks.Application.Authorization;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.RevokeAllMySessions
{
    public sealed record RevokeAllMySessionsCommand : IRequest, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeSessionsRevokeAll;
    }
}
