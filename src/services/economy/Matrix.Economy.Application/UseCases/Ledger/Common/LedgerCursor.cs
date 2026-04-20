namespace Matrix.Economy.Application.UseCases.Ledger.Common
{
    public readonly record struct LedgerCursor(
        long UtcTicks,
        Guid EntryId);
}
