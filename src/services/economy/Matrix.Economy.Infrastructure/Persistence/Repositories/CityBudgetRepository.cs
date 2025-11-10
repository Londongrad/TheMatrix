using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityBudgetRepository(EconomyDbContext dbContext) : ICityBudgetRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityBudget?> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            // В первой версии считаем, что бюджет один.
            return await _dbContext.CityBudgets.SingleOrDefaultAsync(cancellationToken);
        }

        public void Add(CityBudget cityBudget)
        {
            _dbContext.CityBudgets.Add(cityBudget);
        }
    }
}
