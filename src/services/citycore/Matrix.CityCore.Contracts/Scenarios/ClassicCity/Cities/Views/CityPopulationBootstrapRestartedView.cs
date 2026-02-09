namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityPopulationBootstrapRestartedView(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        string SimulationKind);
}
