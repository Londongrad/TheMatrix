using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityRepository(SimulationCoreDbContext dbContext) : ICityRepository
    {
        public Task<City?> GetByIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            // Для команд лучше tracked entity (без AsNoTracking), чтобы изменения сохранились.
            return dbContext.Cities.SingleOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);
        }

        public Task<City?> GetByProvisioningCorrelationIdAsync(
            Guid provisioningCorrelationId,
            CancellationToken cancellationToken)
        {
            return dbContext.Cities
               .AsNoTracking()
               .SingleOrDefaultAsync(
                    predicate: x => x.ProvisioningCorrelationId == provisioningCorrelationId,
                    cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<City>> ListAsync(
            bool includeArchived,
            CancellationToken cancellationToken)
        {
            IQueryable<City> query = dbContext.Cities.AsNoTracking();

            query = includeArchived
                ? query.Where(x => x.Status == CityStatus.Active || x.Status == CityStatus.Archived)
                : query.Where(x => x.Status == CityStatus.Active);

            return await query
               .OrderBy(x => x.CreatedAtUtc)
               .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<City>> ListProvisioningAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Cities
               .AsNoTracking()
               .Where(x => x.Status == CityStatus.Provisioning || x.Status == CityStatus.ProvisioningFailed)
               .OrderByDescending(x => x.Status == CityStatus.ProvisioningFailed)
               .ThenByDescending(x => x.CreatedAtUtc)
               .ToListAsync(cancellationToken);
        }

        public Task AddAsync(
            City city,
            CancellationToken cancellationToken)
        {
            return dbContext.Cities.AddAsync(
                    entity: city,
                    cancellationToken: cancellationToken)
               .AsTask();
        }

        public Task DeleteAsync(
            City city,
            CancellationToken cancellationToken)
        {
            dbContext.Cities.Remove(city);
            return Task.CompletedTask;
        }
    }
}
