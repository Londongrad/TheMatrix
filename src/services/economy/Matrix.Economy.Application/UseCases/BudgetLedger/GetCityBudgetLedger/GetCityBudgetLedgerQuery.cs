using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetLedger.GetCityBudgetLedger
{
    public sealed record GetCityBudgetLedgerQuery(
        Guid CityId,
        int PageNumber,
        int PageSize) : IRequest<PagedResult<BudgetLedgerEntryDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetRead;
    }
}
