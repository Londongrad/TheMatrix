using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityBusinessRepository(EconomyDbContext dbContext) : ICityBusinessRepository
    {
        private readonly EconomyDbContext _dbContext = dbContext;

        public async Task<CityBusiness?> GetByIdAsync(
            Guid businessId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBusinesses.SingleOrDefaultAsync(
                predicate: x => x.Id == businessId,
                cancellationToken: cancellationToken);
        }

        public async Task<CityBusiness?> GetByCityAndExternalReferenceCodeAsync(
            Guid cityId,
            string externalReferenceCode,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBusinesses.SingleOrDefaultAsync(
                predicate: x => x.CityId == cityId && x.ExternalReferenceCode == externalReferenceCode,
                cancellationToken: cancellationToken);
        }

        public async Task<CityBusiness?> GetByCityAndTemplateKeyAsync(
            Guid cityId,
            string templateKey,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBusinesses.SingleOrDefaultAsync(
                predicate: x => x.CityId == cityId && x.TemplateKey == templateKey,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityBusiness>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityBusinesses
               .Where(x => x.CityId == cityId)
               .OrderBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public void Add(CityBusiness cityBusiness)
        {
            _dbContext.CityBusinesses.Add(cityBusiness);
        }
    }
}
