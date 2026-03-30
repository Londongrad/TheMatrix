using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Businesses.GetCityBusinessLedger
{
    public sealed record GetCityBusinessLedgerQuery(
        Guid BusinessId,
        int PageNumber,
        int PageSize) : IRequest<PagedResult<CityBusinessLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBusinessesRead;
    }
}
