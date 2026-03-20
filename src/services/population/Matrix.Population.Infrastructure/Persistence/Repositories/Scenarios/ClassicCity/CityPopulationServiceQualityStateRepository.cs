using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationServiceQualityStateRepository(PopulationDbContext dbContext)
        : ICityPopulationServiceQualityStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationServiceQualityState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationServiceQualityStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationServiceQualityState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationServiceQualityStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationServiceQualityStates
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
