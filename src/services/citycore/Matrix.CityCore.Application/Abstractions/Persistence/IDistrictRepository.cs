using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.CityCore.Application.Abstractions.Persistence
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
