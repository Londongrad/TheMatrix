namespace Matrix.Economy.Api.Contracts.Budget
{
    public sealed record RecordBudgetEntryRequest(
        string Category,
        decimal Amount,
        string Title,
        string? Description,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol);
}
