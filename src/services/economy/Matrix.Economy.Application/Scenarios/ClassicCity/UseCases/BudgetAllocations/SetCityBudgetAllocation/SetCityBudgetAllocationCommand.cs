using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.SetCityBudgetAllocation
{
    public sealed record SetCityBudgetAllocationCommand(
        Guid CityId,
        CityBudgetCategory Category,
        decimal TargetAmount,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<CityBudgetAllocationDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetManage;
    }
}
