namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityProvisioningView(
        Guid CityId,
        CityPopulationBootstrapView PopulationBootstrap,
        CityEconomyBootstrapView EconomyBootstrap);
}
