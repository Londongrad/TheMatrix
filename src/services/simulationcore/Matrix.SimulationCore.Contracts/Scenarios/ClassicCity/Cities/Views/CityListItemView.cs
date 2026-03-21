namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityListItemView(
        Guid CityId,
        Guid SimulationId,
        string Name,
        string SimulationKind,
        string Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? PopulationBootstrapCompletedAtUtc,
        DateTimeOffset? PopulationBootstrapFailedAtUtc,
        string? PopulationBootstrapFailureCode,
        DateTimeOffset? ArchivedAtUtc);
}
