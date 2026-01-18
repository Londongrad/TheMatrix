using Matrix.ApiGateway.DownstreamClients.CityCore.Models;

namespace Matrix.ApiGateway.DownstreamClients.CityCore
{
    public interface ICityCoreApiClient
    {
        Task BootstrapAsync(
            Guid cityId,
            CancellationToken cancellationToken = default);

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
            CityCoreSetClockSpeedRequestDto request,
            CancellationToken cancellationToken = default);

        Task JumpClockAsync(
            Guid cityId,
            CityCoreJumpClockRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
