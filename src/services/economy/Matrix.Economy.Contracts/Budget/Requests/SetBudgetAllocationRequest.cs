namespace Matrix.Economy.Contracts.Budget.Requests
{
    public sealed record SetBudgetAllocationRequest(
        decimal TargetAmount,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol);
}
