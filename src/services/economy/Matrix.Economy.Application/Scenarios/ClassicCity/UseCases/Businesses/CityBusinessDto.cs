namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses
{
    public sealed record CityBusinessDto(
        Guid BusinessId,
        Guid CityId,
        string CreatedAtUtc,
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
}
