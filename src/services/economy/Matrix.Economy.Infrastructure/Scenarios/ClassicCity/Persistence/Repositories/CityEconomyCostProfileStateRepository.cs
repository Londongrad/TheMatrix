using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityEconomyCostProfileStateRepository(EconomyDbContext dbContext)
        : ICityEconomyCostProfileStateRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public Task<CityEconomyCostProfileState?> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.CityEconomyCostProfileStates.FirstOrDefaultAsync(
                predicate: x => x.CityId == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task AddAsync(
            CityEconomyCostProfileState state,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityEconomyCostProfileStates.AddAsync(
                entity: state,
                cancellationToken: cancellationToken);
        }
    }
}
