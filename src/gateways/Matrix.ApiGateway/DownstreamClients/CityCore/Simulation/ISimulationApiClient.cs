using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Simulation
{
    public interface ISimulationApiClient
    {
        Task<SimulationClockView> GetClockAsync(
            Guid simulationId,
            CancellationToken cancellationToken = default);

        Task PauseClockAsync(
            Guid simulationId,
            CancellationToken cancellationToken = default);

        Task ResumeClockAsync(
            Guid simulationId,
            CancellationToken cancellationToken = default);

        Task SetClockSpeedAsync(
            Guid simulationId,
            SetSpeedRequest request,
            CancellationToken cancellationToken = default);

        Task JumpClockAsync(
            Guid simulationId,
            JumpClockRequest request,
            CancellationToken cancellationToken = default);
    }
}
