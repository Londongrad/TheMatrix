using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class DistrictRepository(SimulationCoreDbContext dbContext) : IDistrictRepository
    {
        public async Task<IReadOnlyList<District>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.Districts
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .OrderBy(x => x.Name)
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<District> districts,
            CancellationToken cancellationToken)
        {
            return dbContext.Districts.AddRangeAsync(
                entities: districts,
                cancellationToken: cancellationToken);
        }
    }
}
