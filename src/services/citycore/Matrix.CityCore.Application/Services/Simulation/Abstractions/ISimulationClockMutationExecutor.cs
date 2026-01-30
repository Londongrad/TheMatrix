using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationClockMutationExecutor
    {
        Task<bool> ExecuteAsync(
            CityId cityId,
            Action<SimulationClock> mutate,
            CancellationToken cancellationToken,
            bool allowArchivedCity = false);
    }
}
