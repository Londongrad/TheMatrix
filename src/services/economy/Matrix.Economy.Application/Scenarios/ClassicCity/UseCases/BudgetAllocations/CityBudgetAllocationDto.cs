namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations
{
    public sealed record CityBudgetAllocationDto(
        Guid AllocationId,
        Guid CityId,
        string Category,
        string CreatedAtUtc,
        string UpdatedAtUtc,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal TargetAmount,
        decimal TotalSpent,
        decimal AvailableAmount);
}
