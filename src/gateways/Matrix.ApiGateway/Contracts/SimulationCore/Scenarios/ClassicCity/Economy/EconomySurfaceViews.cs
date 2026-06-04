namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Economy
{
    public sealed record CityBusinessView(
        Guid BusinessId,
        Guid CityId,
        DateTimeOffset CreatedAtUtc,
        string Name,
        string Kind,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal Balance,
        decimal TaxReserve,
        decimal TotalCapitalInjections,
        decimal TotalRetailTurnover,
        decimal TotalNetSalesRevenue,
        decimal TotalOperatingExpenses,
        decimal TotalTaxRemitted);

    public sealed record CityHouseholdAccountView(
        Guid HouseholdAccountId,
        Guid CityId,
        DateTimeOffset CreatedAtUtc,
        string Name,
        string? ExternalReferenceCode,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string UnitSymbol,
        decimal Balance,
        decimal TotalOpeningBalance,
        decimal TotalPayrollIncome,
        decimal TotalConsumerSpending);

    public sealed record BudgetLedgerEntryView(
        Guid EntryId,
        DateTimeOffset OccurredAtUtc,
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

    public sealed record CityBusinessLedgerEntryView(
        Guid EntryId,
        DateTimeOffset OccurredAtUtc,
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

    public sealed record CityHouseholdAccountLedgerEntryView(
        Guid EntryId,
        DateTimeOffset OccurredAtUtc,
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
