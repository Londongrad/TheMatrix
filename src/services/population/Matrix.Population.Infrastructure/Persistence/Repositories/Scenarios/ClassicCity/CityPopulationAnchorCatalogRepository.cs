using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationAnchorCatalogRepository(PopulationDbContext dbContext)
        : ICityPopulationAnchorCatalogRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<IReadOnlyList<CityPopulationAnchorCatalogItem>> ListByCityAsync(
            CityId cityId,
            CityAnchorType? type = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<CityPopulationAnchorCatalogItem> query = _dbContext.CityPopulationAnchorCatalogItems
               .Where(x => x.CityId == cityId);

            if (type.HasValue)
                query = query.Where(x => x.Type == type.Value);

            return await query
               .OrderBy(x => x.Type)
               .ThenBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public async Task AddRangeAsync(
            IReadOnlyCollection<CityPopulationAnchorCatalogItem> items,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationAnchorCatalogItems.AddRangeAsync(
                entities: items,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.CityPopulationAnchorCatalogItems
               .Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
