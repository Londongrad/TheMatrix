using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class HouseholdWriteRepository(PopulationDbContext context) : IHouseholdWriteRepository
    {
        private readonly PopulationDbContext _dbContext = context;

        public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.ClassicCityHouseholdPlacements.ExecuteDeleteAsync(cancellationToken);
            await _dbContext.Households.ExecuteDeleteAsync(cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            List<HouseholdId> householdIds = await _dbContext.ClassicCityHouseholdPlacements
               .Where(x => x.CityId == cityId)
               .Select(x => x.HouseholdId)
               .ToListAsync(cancellationToken);

            if (householdIds.Count == 0)
                return;

            await _dbContext.ClassicCityHouseholdPlacements
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.Households
               .Where(x => householdIds.Contains(x.Id))
               .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task AddRangeAsync(
            IReadOnlyCollection<Household> households,
            IReadOnlyCollection<ClassicCityHouseholdPlacement> householdPlacements,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Households.AddRangeAsync(
                entities: households,
                cancellationToken: cancellationToken);
            await _dbContext.ClassicCityHouseholdPlacements.AddRangeAsync(
                entities: householdPlacements,
                cancellationToken: cancellationToken);
        }
    }
}
