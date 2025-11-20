using Matrix.ApiGateway.DownstreamClients.Economy.Models;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    internal sealed class EconomyApiClient(HttpClient client, ILogger<EconomyApiClient> logger) : IEconomyApiClient
    {
        private readonly HttpClient _client = client;
        private readonly ILogger<EconomyApiClient> _logger = logger;

        private const string SummaryEndpoint = "/api/economy/Budget/summary";
        private const string HealthEndpoint = "/api/economy/Budget/health";

        public async Task<EconomySummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var response = await _client.GetAsync(SummaryEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<EconomySummaryDto>(cancellationToken: cancellationToken);
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            var response = await _client.GetAsync(HealthEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Economy health check failed with status code {StatusCode}", response.StatusCode);
                return false;
            }

            return true;
        }
    }
}