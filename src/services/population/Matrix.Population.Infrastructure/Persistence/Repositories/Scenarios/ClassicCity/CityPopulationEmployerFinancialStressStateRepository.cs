using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationEmployerFinancialStressStateRepository(PopulationDbContext dbContext)
        : ICityPopulationEmployerFinancialStressStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationEmployerFinancialStressState?> GetByCityAndWorkplaceAsync(
            CityId cityId,
            WorkplaceId workplaceId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationEmployerFinancialStressStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId && x.WorkplaceId == workplaceId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityPopulationEmployerFinancialStressState>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityPopulationEmployerFinancialStressStates
               .Where(x => x.CityId == cityId)
               .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationEmployerFinancialStressState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEmployerFinancialStressStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEmployerFinancialStressStates
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
