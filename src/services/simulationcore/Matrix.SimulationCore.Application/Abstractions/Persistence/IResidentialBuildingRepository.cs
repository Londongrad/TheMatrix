using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence
{
    public interface IResidentialBuildingRepository
    {
        Task<IReadOnlyList<ResidentialBuilding>> ListByCityIdAsync(
            CityId cityId,
            DistrictId? districtId,
            CancellationToken cancellationToken);

        Task AddRangeAsync(
            IReadOnlyCollection<ResidentialBuilding> buildings,
            CancellationToken cancellationToken);
    }
}
