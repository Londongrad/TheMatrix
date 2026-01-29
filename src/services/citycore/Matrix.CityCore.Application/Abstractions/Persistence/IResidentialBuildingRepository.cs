using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Topology;

namespace Matrix.CityCore.Application.Abstractions.Persistence
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