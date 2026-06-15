using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed
{
    public sealed record GetCityHouseholdAccountLedgerFeedQuery(
        Guid HouseholdAccountId,
        string? Cursor,
        int PageSize) : IRequest<CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsRead;
    }
}
