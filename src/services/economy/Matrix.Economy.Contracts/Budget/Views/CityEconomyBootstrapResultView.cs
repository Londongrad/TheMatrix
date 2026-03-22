namespace Matrix.Economy.Contracts.Budget.Views
{
    public sealed record CityEconomyBootstrapResultView(
        Guid CityId,
        bool BudgetCreated,
        int CreatedAllocations,
        int CreatedBusinesses,
        string UnitKind,
        string UnitCode,
        string UnitDisplayName,
        string? UnitSymbol);
}
