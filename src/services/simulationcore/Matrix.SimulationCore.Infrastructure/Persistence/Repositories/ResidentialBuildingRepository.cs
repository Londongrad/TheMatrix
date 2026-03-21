using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class ResidentialBuildingRepository(SimulationCoreDbContext dbContext) : IResidentialBuildingRepository
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
