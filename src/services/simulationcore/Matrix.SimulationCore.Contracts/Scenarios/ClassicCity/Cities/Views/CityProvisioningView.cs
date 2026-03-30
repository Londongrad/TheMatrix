namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityProvisioningView(
        Guid CityId,
        string SimulationKind,
        CityPopulationBootstrapView PopulationBootstrap,
        CityEconomyBootstrapView EconomyBootstrap);
}
