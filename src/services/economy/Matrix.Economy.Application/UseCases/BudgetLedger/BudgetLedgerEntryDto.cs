namespace Matrix.Economy.Application.UseCases.BudgetLedger
{
    public sealed record BudgetLedgerEntryDto(
        Guid EntryId,
        string OccurredAtUtc,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        string Kind,
        string Category,
        decimal Amount,
        string Title,
        string Description,
        string Source,
        string? ReferenceCode);
}
