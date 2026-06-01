using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence
{
    public interface IDistrictRepository
    {
        Task<IReadOnlyList<District>> ListByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddRangeAsync(
            IReadOnlyCollection<District> districts,
            CancellationToken cancellationToken);
    }
}
