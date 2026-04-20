using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccountLedgerFeed
{
    public sealed record GetCityHouseholdAccountLedgerFeedQuery(
        Guid HouseholdAccountId,
        string? Cursor,
        int PageSize) : IRequest<CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyHouseholdAccountsRead;
    }
}
