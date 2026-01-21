using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Abstractions.Persistence
{
    public interface ISimulationClockRepository
    {
        Task<SimulationClock?> GetByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);

        Task AddAsync(
            SimulationClock clock,
            CancellationToken cancellationToken);

        Task DeleteByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken);
    }
}
