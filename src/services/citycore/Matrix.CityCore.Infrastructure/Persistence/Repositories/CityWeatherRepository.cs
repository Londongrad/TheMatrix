using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Weather;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityWeatherRepository(CityCoreDbContext dbContext) : ICityWeatherRepository
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
