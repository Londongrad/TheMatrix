using Matrix.ApiGateway.DownstreamClients.CityCore.Models;

namespace Matrix.ApiGateway.DownstreamClients.CityCore
{
    internal sealed class CityCoreApiClient(HttpClient client, ILogger<CityCoreApiClient> logger)
        : ICityCoreApiClient
    {
        private readonly HttpClient _client = client;
        private readonly ILogger<CityCoreApiClient> _logger = logger;

        private const string TimeEndpoint = "/api/citycore/Simulation/time";
        private const string HealthEndpoint = "/api/citycore/Simulation/health";

        public async Task<CitySimulationTimeDto?> GetCurrentTimeAsync(
            CancellationToken cancellationToken = default)
        {
            using var response = await _client.GetAsync(TimeEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get city simulation time. Status code: {StatusCode}",
                    response.StatusCode);

                response.EnsureSuccessStatusCode();
            }

            var dto = await response.Content.ReadFromJsonAsync<CitySimulationTimeDto>(
                cancellationToken: cancellationToken);

            return dto;
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _client.GetAsync(HealthEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CityCore health check failed with status code {StatusCode}",
                    response.StatusCode);

                return false;
            }

            return true;
        }
    }
}