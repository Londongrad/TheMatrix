using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBudgetAllocationRepository(EconomyDbContext dbContext) : ICityBudgetAllocationRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityBudgetAllocation?> GetByCityAndCategoryAsync(
            Guid cityId,
            CityBudgetCategory category,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBudgetAllocations.SingleOrDefaultAsync(
                predicate: x => x.CityId == cityId && x.Category == category,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityBudgetAllocation>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBudgetAllocations
               .Where(x => x.CityId == cityId)
               .OrderBy(x => x.Category)
               .ToListAsync(cancellationToken);
        }

        public void Add(CityBudgetAllocation allocation)
        {
            _dbContext.CityBudgetAllocations.Add(allocation);
        }
    }
}
