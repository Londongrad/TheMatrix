using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.Services.Simulation.Abstractions
{
    public interface ISimulationAdvanceExecutor
    {
        Task<SimulationAdvanceExecutionResult> ExecuteAsync(
            CityId cityId,
            TimeSpan realDelta,
            CancellationToken cancellationToken);
    }
}
