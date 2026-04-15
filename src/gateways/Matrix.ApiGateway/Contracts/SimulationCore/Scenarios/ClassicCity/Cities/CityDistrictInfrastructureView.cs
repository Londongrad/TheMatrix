using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;

namespace Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities
{
    public sealed record CityDistrictInfrastructureView(
        Guid CityId,
        DateTimeOffset GeneratedAtUtc,
        CityDistrictHeatingConditionsView Heating,
        CityDistrictWaterDistributionConditionsView WaterDistribution,
        CityDistrictPowerDistributionConditionsView PowerDistribution,
        CityDistrictSanitationConditionsView Sanitation,
        CityDistrictUtilityIncidentConditionsView UtilityIncidents);
}
