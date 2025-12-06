using Matrix.ApiGateway.DownstreamClients.Economy.Models;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    public interface IEconomyApiClient
    {
        Task<EconomySummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<bool> HealthAsync(CancellationToken cancellationToken = default);
    }
}
