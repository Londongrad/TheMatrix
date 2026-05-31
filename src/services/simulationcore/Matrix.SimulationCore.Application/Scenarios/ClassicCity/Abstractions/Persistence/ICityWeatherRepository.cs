using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence
{
    public interface ICityWeatherRepository
    {
        Task<CityWeather?> GetByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddAsync(
            CityWeather cityWeather,
            CancellationToken cancellationToken);
    }
}
