using Matrix.ApiGateway.DownstreamClients.CityCore.Models;

namespace Matrix.ApiGateway.DownstreamClients.CityCore
{
    public interface ICityCoreApiClient
    {
        Task<CitySimulationTimeDto?> GetCurrentTimeAsync(CancellationToken cancellationToken = default);
        Task<bool> HealthAsync(CancellationToken cancellationToken = default);
    }
}
