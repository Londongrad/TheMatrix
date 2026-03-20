using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationCostOfLivingStateRepository(PopulationDbContext dbContext)
        : ICityPopulationCostOfLivingStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationCostOfLivingState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationCostOfLivingStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationCostOfLivingState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationCostOfLivingStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationCostOfLivingStates
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
