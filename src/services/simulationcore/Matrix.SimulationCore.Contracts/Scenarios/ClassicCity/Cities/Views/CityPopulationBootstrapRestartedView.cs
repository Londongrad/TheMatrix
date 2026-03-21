namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityPopulationBootstrapRestartedView(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        Guid EconomyBootstrapOperationId,
        string SimulationKind);
}
