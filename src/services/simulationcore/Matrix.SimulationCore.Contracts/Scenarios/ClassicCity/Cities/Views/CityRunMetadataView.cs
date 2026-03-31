namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityRunMetadataView(
        Guid RunId,
        string SimulationSeed,
        string ScenarioModelSetVersion);
}
