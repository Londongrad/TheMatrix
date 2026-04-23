using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence
{
    public interface ICityAnchorRepository
    {
        Task<CityAnchor?> GetByIdAsync(
            CityAnchorId anchorId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<CityAnchor>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddRangeAsync(
            IReadOnlyCollection<CityAnchor> anchors,
            CancellationToken cancellationToken);
    }
}
