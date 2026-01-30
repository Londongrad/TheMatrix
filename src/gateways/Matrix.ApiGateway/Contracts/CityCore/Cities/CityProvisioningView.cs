namespace Matrix.ApiGateway.Contracts.CityCore.Cities
{
    public sealed record CityProvisioningView(
        Guid CityId,
        CityPopulationBootstrapView PopulationBootstrap);
}
