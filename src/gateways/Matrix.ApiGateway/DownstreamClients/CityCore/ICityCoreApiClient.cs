using Matrix.ApiGateway.DownstreamClients.CityCore.Models;
using Matrix.CityCore.Contracts.Simulation.Requests;

namespace Matrix.ApiGateway.DownstreamClients.CityCore
{
    public interface ICityCoreApiClient
    {
        Task<BootstrapCityResponseDto> BootstrapAsync(CancellationToken cancellationToken = default);

        Task<CityCoreClockResponseDto> GetClockAsync(
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
