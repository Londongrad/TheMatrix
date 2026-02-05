using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class CityPopulationWeatherExposureStateRepository(PopulationDbContext dbContext)
        : ICityPopulationWeatherExposureStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationWeatherExposureState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationWeatherExposureStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationWeatherExposureState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationWeatherExposureStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationWeatherExposureStates
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
