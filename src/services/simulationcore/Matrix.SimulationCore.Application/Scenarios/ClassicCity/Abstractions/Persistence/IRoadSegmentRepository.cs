using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence
{
    public interface IRoadSegmentRepository
    {
        Task<IReadOnlyList<RoadSegment>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddRangeAsync(
            IReadOnlyCollection<RoadSegment> roadSegments,
            CancellationToken cancellationToken);
    }
}
