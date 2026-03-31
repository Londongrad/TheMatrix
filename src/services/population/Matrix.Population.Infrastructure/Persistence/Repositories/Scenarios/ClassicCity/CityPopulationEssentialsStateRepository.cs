using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationEssentialsStateRepository(PopulationDbContext dbContext)
        : ICityPopulationEssentialsStateRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public Task<CityPopulationEssentialsState?> GetByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityPopulationEssentialsStates.FirstOrDefaultAsync(
                x => x.CityId == cityId,
                cancellationToken);
        }

        public async Task AddAsync(
            CityPopulationEssentialsState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEssentialsStates.AddAsync(state, cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationEssentialsStates
                .Where(x => x.CityId == cityId)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
