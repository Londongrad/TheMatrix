using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityRepository(CityCoreDbContext dbContext) : ICityRepository
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

        public async Task<IReadOnlyList<City>> ListAsync(
            bool includeArchived,
            CancellationToken cancellationToken)
        {
            IQueryable<City> query = dbContext.Cities.AsNoTracking();

            if (!includeArchived)
                query = query.Where(x => x.Status == CityStatus.Active);

            return await query
               .OrderBy(x => x.CreatedAtUtc)
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
