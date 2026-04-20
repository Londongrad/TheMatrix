using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedgerFeed
{
    public sealed record GetCityBusinessLedgerFeedQuery(
        Guid BusinessId,
        string? Cursor,
        int PageSize) : IRequest<CursorPagedResult<CityBusinessLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesRead;
    }
}
