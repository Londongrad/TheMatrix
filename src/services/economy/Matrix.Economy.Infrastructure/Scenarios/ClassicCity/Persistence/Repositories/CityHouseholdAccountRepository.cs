using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityHouseholdAccountRepository(EconomyDbContext dbContext) : ICityHouseholdAccountRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityHouseholdAccount?> GetByIdAsync(
            Guid householdAccountId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdAccounts.SingleOrDefaultAsync(
                predicate: x => x.Id == householdAccountId,
                cancellationToken: cancellationToken);
        }

        public async Task<CityHouseholdAccount?> GetByCityAndExternalReferenceCodeAsync(
            Guid cityId,
            string externalReferenceCode,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdAccounts.SingleOrDefaultAsync(
                predicate: x => x.CityId == cityId && x.ExternalReferenceCode == externalReferenceCode,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdAccount>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
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
