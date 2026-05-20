using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed record GetMySecurityActivityQuery(
        string? Cursor,
        int PageSize)
        : IRequest<CursorPagedResult<SecurityActivityItemResult>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.IdentityMeSessionsRead;
    }
}
