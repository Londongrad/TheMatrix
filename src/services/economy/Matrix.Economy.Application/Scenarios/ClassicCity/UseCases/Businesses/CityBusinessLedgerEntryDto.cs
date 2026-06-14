namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses
{
    public sealed record CityBusinessLedgerEntryDto(
        Guid EntryId,
        string OccurredAtUtc,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        string Kind,
        decimal Amount,
        decimal TaxAmount,
        string Title,
        string Description,
        string Source,
        string? ReferenceCode);
}
