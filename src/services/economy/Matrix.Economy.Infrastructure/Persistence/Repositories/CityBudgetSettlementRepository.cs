using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityBudgetSettlementRepository(EconomyDbContext dbContext)
        : ICityBudgetSettlementRepository
    {
        public Task<bool> ExistsAsync(Guid cityId, long tickId, CancellationToken cancellationToken = default)
        {
            return dbContext.CityBudgetSettlements.AnyAsync(
                x => x.CityId == cityId && x.TickId == tickId,
                cancellationToken);
        }

        public Task AddAsync(CityBudgetSettlement settlement, CancellationToken cancellationToken = default)
        {
            dbContext.CityBudgetSettlements.Add(settlement);
            return Task.CompletedTask;
        }
    }
}
