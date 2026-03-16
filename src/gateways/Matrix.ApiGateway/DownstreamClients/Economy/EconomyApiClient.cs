using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.ApiGateway.DownstreamClients.Economy.Models;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    internal sealed class EconomyApiClient(
        HttpClient client,
        ILogger<EconomyApiClient> logger) : IEconomyApiClient
    {
        private const string SummaryEndpoint = "/api/economy/Budget/summary";
        private const string CitySummaryEndpointTemplate = "/api/economy/Budget/cities/{0}/summary";
        private const string CityBootstrapEndpointTemplate = "/api/economy/Budget/cities/{0}/bootstrap";
        private const string HealthEndpoint = "/api/economy/Budget/health";
        private readonly HttpClient _client = client;
        private readonly ILogger<EconomyApiClient> _logger = logger;

        public async Task<EconomySummaryDto?> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: SummaryEndpoint,
                    cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<EconomySummaryDto>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: SummaryEndpoint);
        }

        public async Task<EconomySummaryDto?> GetCitySummaryAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: string.Format(
                        format: CitySummaryEndpointTemplate,
                        arg0: cityId),
                    cancellationToken: cancellationToken);

            string url = string.Format(
                format: CitySummaryEndpointTemplate,
                arg0: cityId);

            return await response.ReadJsonOrThrowDownstreamAsync<EconomySummaryDto>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEconomyBootstrapResultDto> InitializeCityEconomyAsync(
            Guid cityId,
            InitializeCityEconomyRequestDto request,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: CityBootstrapEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEconomyBootstrapResultDto>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: HealthEndpoint,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    message: "Economy health check failed with status code {StatusCode}",
                    response.StatusCode);
                return false;
            }

            return true;
        }
    }
}
