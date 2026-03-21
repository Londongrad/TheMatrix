using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions
{
    public interface IWeatherAdvanceExecutor
    {
        Task<CityWeather?> AdvanceAsync(
            CityId cityId,
            SimTime evaluatedAt,
            CancellationToken cancellationToken);
    }
}
