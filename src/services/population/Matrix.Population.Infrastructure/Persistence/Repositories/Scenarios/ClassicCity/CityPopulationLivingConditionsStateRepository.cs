using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationLivingConditionsStateRepository(PopulationDbContext dbContext)
        : ICityPopulationLivingConditionsStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationLivingConditionsState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationLivingConditionsStates.FirstOrDefaultAsync(
                x => x.CityId == cityId,
                cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationLivingConditionsState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationLivingConditionsStates.AddAsync(state, cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationLivingConditionsStates
                .Where(x => x.CityId == cityId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
