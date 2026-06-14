using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetRevenue
{
    public sealed record RecordCityBudgetRevenueCommand(
        Guid CityId,
        CityBudgetCategory Category,
        decimal Amount,
        string Title,
        string? Description,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<BudgetLedgerEntryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetManage;
    }
}
