using Matrix.Economy.Contracts.Budget.Views;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    public interface IEconomyApiClient
    {
        Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task<bool> HealthAsync(CancellationToken cancellationToken = default);
    }
}
