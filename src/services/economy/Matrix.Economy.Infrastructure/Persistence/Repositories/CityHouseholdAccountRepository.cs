using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityHouseholdAccountRepository(EconomyDbContext dbContext) : ICityHouseholdAccountRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityHouseholdAccount?> GetByIdAsync(Guid householdAccountId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdAccounts.SingleOrDefaultAsync(x => x.Id == householdAccountId, cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdAccount>> ListByCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdAccounts
                .Where(x => x.CityId == cityId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public void Add(CityHouseholdAccount householdAccount)
        {
            _dbContext.CityHouseholdAccounts.Add(householdAccount);
        }
    }
}
