using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;

namespace Matrix.ApiGateway.DownstreamClients.Economy
{
    internal sealed class EconomyApiClient(
        HttpClient client,
        ILogger<EconomyApiClient> logger) : IEconomyApiClient
    {
        private const string SummaryEndpoint = "/api/economy/Budget/summary";
        private const string CitySummaryEndpointTemplate = "/api/economy/Budget/cities/{0}/summary";
        private const string CityBootstrapEndpointTemplate = "/api/economy/Budget/cities/{0}/bootstrap";
        private const string ReadyEndpoint = "/health/ready";
        private readonly HttpClient _client = client;
        private readonly ILogger<EconomyApiClient> _logger = logger;

        public async Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: SummaryEndpoint,
                    cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<EconomySummaryView>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: SummaryEndpoint);
        }

        public async Task<EconomySummaryView?> GetCitySummaryAsync(
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

            return await response.ReadJsonOrThrowDownstreamAsync<EconomySummaryView>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityEconomyBootstrapResultView> InitializeCityEconomyAsync(
            Guid cityId,
            InitializeCityEconomyRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: CityBootstrapEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityEconomyBootstrapResultView>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response =
                await _client.GetAsync(
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
