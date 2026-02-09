using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;

namespace Matrix.CityCore.Application.Abstractions.Persistence
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
