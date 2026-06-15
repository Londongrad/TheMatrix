using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger.GetCityBudgetLedgerFeed
{
    public sealed record GetCityBudgetLedgerFeedQuery(
        Guid CityId,
        string? Cursor,
        int PageSize) : IRequest<CursorPagedResult<BudgetLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetRead;
    }
}
