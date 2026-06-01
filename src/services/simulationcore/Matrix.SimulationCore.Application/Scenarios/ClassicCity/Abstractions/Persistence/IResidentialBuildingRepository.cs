using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence
{
    public interface IResidentialBuildingRepository
    {
        Task<ResidentialBuilding?> GetByIdAsync(
            ResidentialBuildingId buildingId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ResidentialBuilding>> ListByCityIdAsync(
            CityId cityId,
            DistrictId? districtId,
            CancellationToken cancellationToken);

        Task AddRangeAsync(
            IReadOnlyCollection<ResidentialBuilding> buildings,
            CancellationToken cancellationToken);
    }
}
