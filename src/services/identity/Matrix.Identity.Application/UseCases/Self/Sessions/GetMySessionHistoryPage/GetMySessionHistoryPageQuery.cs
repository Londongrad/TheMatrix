using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Authorization.Permissions;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage
{
    public sealed record GetMySessionHistoryPageQuery(Pagination Pagination)
        : IRequest<PagedResult<MySessionResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeSessionsRead;
    }
}
