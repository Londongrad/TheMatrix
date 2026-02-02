using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class CityPopulationWeatherImpactStateRepository(PopulationDbContext dbContext)
        : ICityPopulationWeatherImpactStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationWeatherImpactState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationWeatherImpactStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationWeatherImpactState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationWeatherImpactStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }
    }
}
