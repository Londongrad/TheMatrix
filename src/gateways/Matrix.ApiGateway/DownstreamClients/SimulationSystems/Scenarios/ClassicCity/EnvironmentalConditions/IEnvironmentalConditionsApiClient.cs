using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions
{
    public interface IEnvironmentalConditionsApiClient
    {
        Task<CityEnvironmentalConditionsView?> GetCityEnvironmentalConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityDistrictHeatingConditionsView?> GetCityDistrictHeatingConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityDistrictWaterDistributionConditionsView?> GetCityDistrictWaterDistributionConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityDistrictPowerDistributionConditionsView?> GetCityDistrictPowerDistributionConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityDistrictSanitationConditionsView?> GetCityDistrictSanitationConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityDistrictUtilityIncidentConditionsView?> GetCityDistrictUtilityIncidentConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task<CityUtilityIncidentStatusView> DispatchCityUtilityIncidentResponseAsync(
            Guid cityId,
            DispatchCityUtilityIncidentResponseRequest request,
            CancellationToken cancellationToken = default);
    }
}
