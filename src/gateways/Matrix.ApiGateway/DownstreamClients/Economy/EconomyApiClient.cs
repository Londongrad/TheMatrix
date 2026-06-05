using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Economy.Contracts.Budget.Views;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    internal sealed class EconomyApiClient(
        HttpClient client,
        ILogger<EconomyApiClient> logger) : IEconomyApiClient
    {
        private const string SummaryEndpoint = "/api/economy/Budget/summary";
        private const string ReadyEndpoint = "/health/ready";
        private readonly HttpClient _client = client;
        private readonly ILogger<EconomyApiClient> _logger = logger;

        public async Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await _client.GetAsync(
                requestUri: SummaryEndpoint,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<EconomySummaryView>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: SummaryEndpoint);
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await _client.GetAsync(
                requestUri: ReadyEndpoint,
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    message: "Economy ready probe failed with status code {StatusCode}",
                    response.StatusCode);
                return false;
            }

            return true;
        }
    }
}
