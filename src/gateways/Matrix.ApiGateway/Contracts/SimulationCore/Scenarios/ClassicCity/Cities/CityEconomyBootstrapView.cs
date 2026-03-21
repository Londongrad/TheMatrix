namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityEconomyBootstrapView(
        Guid OperationId,
        string Status,
        string? FailureCode,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol);
}
