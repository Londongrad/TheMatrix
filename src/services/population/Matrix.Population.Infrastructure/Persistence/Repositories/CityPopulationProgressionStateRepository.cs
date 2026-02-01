using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class CityPopulationProgressionStateRepository(PopulationDbContext dbContext)
        : ICityPopulationProgressionStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationProgressionState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationProgressionStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationProgressionState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationProgressionStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }
    }
}
