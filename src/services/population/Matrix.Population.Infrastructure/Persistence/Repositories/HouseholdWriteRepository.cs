using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class HouseholdWriteRepository(PopulationDbContext context) : IHouseholdWriteRepository
    {
        private readonly PopulationDbContext _dbContext = context;

        public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.Households.ExecuteDeleteAsync(cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Households
                .Where(x => x.CityId == cityId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task AddRangeAsync(
            IReadOnlyCollection<Household> households,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Households.AddRangeAsync(
                entities: households,
                cancellationToken: cancellationToken);
        }
    }
}