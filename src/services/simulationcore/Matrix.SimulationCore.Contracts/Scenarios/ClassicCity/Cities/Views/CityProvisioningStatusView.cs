namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityProvisioningStatusView(
        Guid CityId,
        string Status,
        Guid PopulationBootstrapOperationId,
        Guid EconomyBootstrapOperationId,
        string? PopulationBootstrapFailureCode,
        string? EconomyBootstrapFailureCode,
        DateTimeOffset? PopulationBootstrapCompletedAtUtc,
        DateTimeOffset? EconomyBootstrapCompletedAtUtc,
        DateTimeOffset? PopulationBootstrapFailedAtUtc,
        DateTimeOffset? EconomyBootstrapFailedAtUtc);
}
