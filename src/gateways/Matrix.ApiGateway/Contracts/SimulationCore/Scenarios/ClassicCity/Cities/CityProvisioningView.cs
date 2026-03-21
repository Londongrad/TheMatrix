namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityProvisioningView(
        Guid CityId,
        string SimulationKind,
        CityPopulationBootstrapView PopulationBootstrap,
        CityEconomyBootstrapView EconomyBootstrap);
}
