namespace Matrix.CityCore.Contracts.Cities.Views
{
    public sealed record CityPopulationBootstrapRestartedView(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        string SimulationKind);
}
