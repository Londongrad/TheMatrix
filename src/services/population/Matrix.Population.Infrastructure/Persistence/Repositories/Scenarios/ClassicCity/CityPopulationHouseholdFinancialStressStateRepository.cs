using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationHouseholdFinancialStressStateRepository(PopulationDbContext dbContext)
        : ICityPopulationHouseholdFinancialStressStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationHouseholdFinancialStressState?> GetByCityAndHouseholdAsync(
            CityId cityId,
            HouseholdId householdId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationHouseholdFinancialStressStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId && x.HouseholdId == householdId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityPopulationHouseholdFinancialStressState>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityPopulationHouseholdFinancialStressStates
               .Where(x => x.CityId == cityId)
               .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationHouseholdFinancialStressState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationHouseholdFinancialStressStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationHouseholdFinancialStressStates
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
