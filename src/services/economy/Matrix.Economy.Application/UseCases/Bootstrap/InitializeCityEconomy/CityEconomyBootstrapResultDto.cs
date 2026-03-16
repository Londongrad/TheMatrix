namespace Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed record CityEconomyBootstrapResultDto(
        Guid CityId,
        bool BudgetCreated,
        int CreatedAllocations,
        int CreatedBusinesses,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string? UnitSymbol);
}
