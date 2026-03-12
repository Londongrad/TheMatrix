using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence.Repositories
{
    public sealed class CityHouseholdObligationRepository(EconomyDbContext dbContext) : ICityHouseholdObligationRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityHouseholdObligation?> GetByIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations.SingleOrDefaultAsync(x => x.Id == obligationId, cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdObligation>> ListByCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations
                .Where(x => x.CityId == cityId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CityHouseholdObligation>> ListByHouseholdAsync(Guid householdAccountId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityHouseholdObligations
                .Where(x => x.HouseholdAccountId == householdAccountId)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public void Add(CityHouseholdObligation obligation)
        {
            _dbContext.CityHouseholdObligations.Add(obligation);
        }
    }
}
