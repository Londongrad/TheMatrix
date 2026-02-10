using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class CityPopulationEnvironmentRepository(PopulationDbContext dbContext)
        : ICityPopulationEnvironmentRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationEnvironment?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationEnvironments.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEnvironments.AddAsync(
                entity: environment,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEnvironments
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
