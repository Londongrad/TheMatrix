namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts
{
    public sealed record CityHouseholdAccountLedgerEntryDto(
        Guid EntryId,
        string OccurredAtUtc,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        string Kind,
        decimal Amount,
        string Title,
        string Description,
        string Source,
        string? ReferenceCode);
}
