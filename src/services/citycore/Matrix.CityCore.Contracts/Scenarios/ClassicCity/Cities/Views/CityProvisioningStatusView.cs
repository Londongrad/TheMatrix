namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityProvisioningStatusView(
        Guid CityId,
        string Status,
        Guid PopulationBootstrapOperationId,
        string? PopulationBootstrapFailureCode,
        DateTimeOffset? PopulationBootstrapCompletedAtUtc,
        DateTimeOffset? PopulationBootstrapFailedAtUtc);
}
