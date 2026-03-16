namespace Matrix.ApiGateway.DownstreamClients.Economy.Models
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
