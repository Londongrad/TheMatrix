using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBudgetSettlementRepository(EconomyDbContext dbContext)
        : ICityBudgetSettlementRepository
    {
        public Task<bool> ExistsAsync(
            Guid cityId,
            long tickId,
            CancellationToken cancellationToken = default)
        {
            return dbContext.CityBudgetSettlements.AnyAsync(
                predicate: x => x.CityId == cityId && x.TickId == tickId,
                cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            CityBudgetSettlement settlement,
            CancellationToken cancellationToken = default)
        {
            dbContext.CityBudgetSettlements.Add(settlement);
            return Task.CompletedTask;
        }
    }
}
