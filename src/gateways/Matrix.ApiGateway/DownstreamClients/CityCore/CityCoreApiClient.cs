using Matrix.ApiGateway.DownstreamClients.CityCore.Models;

namespace Matrix.ApiGateway.DownstreamClients.CityCore
{
    internal sealed class CityCoreApiClient(HttpClient client, ILogger<CityCoreApiClient> logger)
        : ICityCoreApiClient
    {
        private const string TimeEndpoint = "/api/citycore/Simulation/time";
        private const string HealthEndpoint = "/api/citycore/Simulation/health";
        private readonly HttpClient _client = client;
        private readonly ILogger<CityCoreApiClient> _logger = logger;

        public async Task<CitySimulationTimeDto?> GetCurrentTimeAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await _client.GetAsync(requestUri: TimeEndpoint, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    message: "Failed to get city simulation time. Status code: {StatusCode}",
                    response.StatusCode);

                response.EnsureSuccessStatusCode();
            }

            CitySimulationTimeDto? dto = await response.Content.ReadFromJsonAsync<CitySimulationTimeDto>(
                cancellationToken: cancellationToken);

            return dto;
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response =
                await _client.GetAsync(requestUri: HealthEndpoint, cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    message: "CityCore health check failed with status code {StatusCode}",
                    response.StatusCode);

                return false;
            }

            return true;
        }
    }
}
