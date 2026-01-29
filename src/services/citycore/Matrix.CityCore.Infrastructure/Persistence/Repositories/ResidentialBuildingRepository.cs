using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class ResidentialBuildingRepository(CityCoreDbContext dbContext) : IResidentialBuildingRepository
    {
        public async Task<IReadOnlyList<ResidentialBuilding>> ListByCityIdAsync(
            CityId cityId,
            DistrictId? districtId,
            CancellationToken cancellationToken)
        {
            IQueryable<ResidentialBuilding> query = dbContext.ResidentialBuildings
               .AsNoTracking()
               .Where(x => x.CityId == cityId);

            if (districtId.HasValue)
                query = query.Where(x => x.DistrictId == districtId.Value);

            return await query
               .OrderBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<ResidentialBuilding> buildings,
            CancellationToken cancellationToken)
        {
            return dbContext.ResidentialBuildings.AddRangeAsync(
                entities: buildings,
                cancellationToken: cancellationToken);
        }
    }
}