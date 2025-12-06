using Matrix.ApiGateway.DownstreamClients.Economy.Models;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    internal sealed class EconomyApiClient(HttpClient client, ILogger<EconomyApiClient> logger) : IEconomyApiClient
    {
        private const string SummaryEndpoint = "/api/economy/Budget/summary";
        private const string HealthEndpoint = "/api/economy/Budget/health";
        private readonly HttpClient _client = client;
        private readonly ILogger<EconomyApiClient> _logger = logger;

        public async Task<EconomySummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(requestUri: SummaryEndpoint, cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<EconomySummaryDto>(cancellationToken: cancellationToken);
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(requestUri: HealthEndpoint, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(message: "Economy health check failed with status code {StatusCode}",
                    response.StatusCode);
                return false;
            }

            return true;
        }
    }
}
