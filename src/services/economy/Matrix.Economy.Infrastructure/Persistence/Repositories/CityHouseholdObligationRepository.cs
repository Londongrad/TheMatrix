using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityHouseholdObligationRepository(EconomyDbContext dbContext)
        : ICityHouseholdObligationRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityHouseholdObligation?> GetByIdAsync(
            Guid obligationId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations.SingleOrDefaultAsync(
                predicate: x => x.Id == obligationId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdObligation>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations
               .Where(x => x.CityId == cityId)
               .OrderBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdObligation>> ListDueByCityAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations
               .Where(x => x.CityId == cityId && x.IsActive && x.NextChargeDueAtUtc <= asOfUtc)
               .OrderBy(x => x.NextChargeDueAtUtc)
               .ThenBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdObligation>> ListByHouseholdAsync(
            Guid householdAccountId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations
               .Where(x => x.HouseholdAccountId == householdAccountId)
               .OrderBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdObligation>> ListByHouseholdsAsync(
            IReadOnlyCollection<Guid> householdAccountIds,
            CancellationToken cancellationToken = default)
        {
            if (householdAccountIds.Count == 0)
                return [];

            return await _dbContext.CityHouseholdObligations
               .Where(x => householdAccountIds.Contains(x.HouseholdAccountId))
               .OrderBy(x => x.HouseholdAccountId)
               .ThenBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public void Add(CityHouseholdObligation obligation)
        {
            _dbContext.CityHouseholdObligations.Add(obligation);
        }
    }
}
