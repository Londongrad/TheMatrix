using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityBudgetRepository(EconomyDbContext dbContext) : ICityBudgetRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityBudget?> GetByCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBudgets.SingleOrDefaultAsync(x => x.CityId == cityId, cancellationToken);
        }

        public async Task<IReadOnlyList<CityBudget>> ListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBudgets.ToListAsync(cancellationToken);
        }

        public void Add(CityBudget cityBudget)
        {
            _dbContext.CityBudgets.Add(cityBudget);
        }
    }
}
