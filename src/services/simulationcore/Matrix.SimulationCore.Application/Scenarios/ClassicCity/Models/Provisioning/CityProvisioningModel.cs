namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning
{
    public sealed record CityProvisioningModel(
        Guid CityId,
        CityPopulationBootstrapModel PopulationBootstrap,
        CityEconomyBootstrapModel EconomyBootstrap);
}
