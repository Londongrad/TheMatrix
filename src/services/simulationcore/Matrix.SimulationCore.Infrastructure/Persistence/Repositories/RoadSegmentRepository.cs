using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories
{
    public sealed class RoadSegmentRepository(SimulationCoreDbContext dbContext) : IRoadSegmentRepository
    {
        public async Task<IReadOnlyList<RoadSegment>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return await dbContext.RoadSegments
               .AsNoTracking()
               .Where(x => x.CityId == cityId)
               .ToListAsync(cancellationToken);
        }

        public Task AddRangeAsync(
            IReadOnlyCollection<RoadSegment> roadSegments,
            CancellationToken cancellationToken)
        {
            return dbContext.RoadSegments.AddRangeAsync(
                entities: roadSegments,
                cancellationToken: cancellationToken);
        }
    }
}
