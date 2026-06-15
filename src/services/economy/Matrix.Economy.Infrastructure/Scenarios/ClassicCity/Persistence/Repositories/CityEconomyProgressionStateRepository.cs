using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityEconomyProgressionStateRepository(EconomyDbContext dbContext)
        : ICityEconomyProgressionStateRepository
    {
        public Task<CityEconomyProgressionState?> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.CityEconomyProgressionStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            CityEconomyProgressionState state,
            CancellationToken cancellationToken = default)
        {
            dbContext.CityEconomyProgressionStates.Add(state);
            return Task.CompletedTask;
        }
    }
}
