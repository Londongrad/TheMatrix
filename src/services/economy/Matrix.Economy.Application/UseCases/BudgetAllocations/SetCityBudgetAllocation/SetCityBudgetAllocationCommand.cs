using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetAllocations.SetCityBudgetAllocation
{
    public sealed record SetCityBudgetAllocationCommand(
        Guid CityId,
        CityBudgetCategory Category,
        decimal TargetAmount,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol) : IRequest<CityBudgetAllocationDto>;
}
