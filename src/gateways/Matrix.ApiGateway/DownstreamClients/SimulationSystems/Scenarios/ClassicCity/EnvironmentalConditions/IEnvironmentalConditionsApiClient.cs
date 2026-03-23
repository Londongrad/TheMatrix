using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;

namespace Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions
{
    public interface IEnvironmentalConditionsApiClient
    {
        Task<CityEnvironmentalConditionsView?> GetCityEnvironmentalConditionsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);
    }
}
