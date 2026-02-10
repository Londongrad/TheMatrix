using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class CityPopulationDeletionStateRepository(PopulationDbContext dbContext)
        : ICityPopulationDeletionStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationDeletionState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationDeletionStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationDeletionState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationDeletionStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }
    }
}
