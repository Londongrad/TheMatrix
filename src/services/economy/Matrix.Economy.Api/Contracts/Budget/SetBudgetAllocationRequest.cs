namespace Matrix.Economy.Api.Contracts.Budget
{
    public sealed record SetBudgetAllocationRequest(
        decimal TargetAmount,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol);
}
