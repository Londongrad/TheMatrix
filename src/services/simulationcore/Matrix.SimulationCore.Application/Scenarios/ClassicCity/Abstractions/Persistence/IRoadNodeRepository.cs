using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence
{
    public interface IRoadNodeRepository
    {
        Task<IReadOnlyList<RoadNode>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddRangeAsync(
            IReadOnlyCollection<RoadNode> roadNodes,
            CancellationToken cancellationToken);
    }
}
