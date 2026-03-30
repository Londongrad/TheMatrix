using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedger
{
    public sealed record GetCityHouseholdAccountLedgerQuery(
        Guid HouseholdAccountId,
        int PageNumber,
        int PageSize) : IRequest<PagedResult<CityHouseholdAccountLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsRead;
    }
}
