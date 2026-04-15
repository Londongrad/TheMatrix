using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;

namespace Matrix.SimulationCore.Application.Abstractions.Persistence
{
    public interface ICityActiveTripRepository
    {
        Task<IReadOnlyList<CityActiveTrip>> ListActiveByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<CityActiveTrip>> ListActiveForUpdateByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddAsync(
            CityActiveTrip trip,
            CancellationToken cancellationToken);
    }
}
