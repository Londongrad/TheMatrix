namespace Matrix.Economy.Application.UseCases.BudgetLedger
{
    public sealed record BudgetLedgerEntryDto(
        Guid EntryId,
        string OccurredAtUtc,
        string Kind,
        string Category,
        decimal Amount,
        string Title,
        string Description,
        string Source,
        string? ReferenceCode);
}
