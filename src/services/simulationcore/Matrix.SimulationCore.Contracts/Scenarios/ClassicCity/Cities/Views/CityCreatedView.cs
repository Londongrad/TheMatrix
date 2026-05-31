namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityCreatedView(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        Guid EconomyBootstrapOperationId);
}
