namespace Matrix.ApiGateway.Contracts.CityCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityProvisioningView(
        Guid CityId,
        string SimulationKind,
        CityPopulationBootstrapView PopulationBootstrap);
}
