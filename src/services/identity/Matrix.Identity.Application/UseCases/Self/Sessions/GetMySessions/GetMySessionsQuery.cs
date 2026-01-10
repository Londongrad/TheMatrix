using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions
{
    public sealed record GetMySessionsQuery
        : IRequest<IReadOnlyCollection<MySessionResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeSessionsRead;
    }
}
