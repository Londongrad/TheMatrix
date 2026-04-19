namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning
{
    public sealed record CityEconomyBootstrapModel(
        Guid OperationId,
        string Status,
        string? FailureCode,
        string? UnitKind,
        string? UnitCode,
        string? UnitDisplayName,
        string? UnitSymbol);
}
