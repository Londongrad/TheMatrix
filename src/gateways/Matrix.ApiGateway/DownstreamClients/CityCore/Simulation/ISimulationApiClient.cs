using Matrix.CityCore.Contracts.Simulation.Requests;
using Matrix.CityCore.Contracts.Simulation.Views;

namespace Matrix.ApiGateway.DownstreamClients.CityCore.Simulation
{
    public interface ISimulationApiClient
    {
        Task<SimulationClockView?> GetClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task PauseClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task ResumeClockAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

        Task SetClockSpeedAsync(
            Guid cityId,
            SetSpeedRequest request,
            CancellationToken cancellationToken = default);

        Task JumpClockAsync(
            Guid cityId,
            JumpClockRequest request,
            CancellationToken cancellationToken = default);
    }
}
