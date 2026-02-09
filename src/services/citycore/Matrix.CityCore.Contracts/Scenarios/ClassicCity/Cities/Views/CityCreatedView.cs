namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityCreatedView(
        Guid CityId,
        Guid PopulationBootstrapOperationId,
        string SimulationKind);
}
