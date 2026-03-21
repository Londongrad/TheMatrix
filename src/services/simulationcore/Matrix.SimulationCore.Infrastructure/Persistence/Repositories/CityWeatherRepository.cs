using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityWeatherRepository(SimulationCoreDbContext dbContext) : ICityWeatherRepository
    {
        public Task<CityWeather?> GetByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return dbContext.CityWeathers.SingleOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            CityWeather cityWeather,
            CancellationToken cancellationToken)
        {
            return dbContext.CityWeathers.AddAsync(
                    entity: cityWeather,
                    cancellationToken: cancellationToken)
               .AsTask();
        }
    }
}
