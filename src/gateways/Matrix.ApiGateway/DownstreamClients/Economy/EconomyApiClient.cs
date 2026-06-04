using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Economy;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
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

        private const string CityOperationalPressureEndpointTemplate =
            "/api/economy/Budget/cities/{0}/operational-pressure";

        private const string CityBusinessesEndpointTemplate = "/api/economy/Business/cities/{0}";
        private const string CityHouseholdAccountsEndpointTemplate = "/api/economy/HouseholdAccounts/cities/{0}";
        private const string CityBudgetLedgerFeedEndpointTemplate = "/api/economy/Budget/cities/{0}/ledger-feed";
        private const string CityBusinessLedgerFeedEndpointTemplate = "/api/economy/Business/{0}/ledger-feed";

        private const string CityHouseholdAccountLedgerFeedEndpointTemplate =
            "/api/economy/HouseholdAccounts/{0}/ledger-feed";

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

        public async Task<CityOperationalBudgetPressureView?> GetCityOperationalBudgetPressureAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: CityOperationalPressureEndpointTemplate,
                arg0: cityId);

            HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: url,
                    cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityOperationalBudgetPressureView>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyList<CityBusinessView>> GetCityBusinessesAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: CityBusinessesEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<CityBusinessView>>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyList<CityHouseholdAccountView>> GetCityHouseholdAccountsAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            string url = string.Format(
                format: CityHouseholdAccountsEndpointTemplate,
                arg0: cityId);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyList<CityHouseholdAccountView>>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CursorPagedResult<BudgetLedgerEntryView>> GetCityBudgetLedgerFeedAsync(
            Guid cityId,
            string? cursor = null,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            string url = BuildCursorFeedUrl(
                baseUrl: string.Format(
                    format: CityBudgetLedgerFeedEndpointTemplate,
                    arg0: cityId),
                cursor: cursor,
                pageSize: pageSize);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CursorPagedResult<BudgetLedgerEntryView>>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CursorPagedResult<CityBusinessLedgerEntryView>> GetCityBusinessLedgerFeedAsync(
            Guid businessId,
            string? cursor = null,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            string url = BuildCursorFeedUrl(
                baseUrl: string.Format(
                    format: CityBusinessLedgerFeedEndpointTemplate,
                    arg0: businessId),
                cursor: cursor,
                pageSize: pageSize);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CursorPagedResult<CityBusinessLedgerEntryView>>(
                serviceName: DownstreamServiceNames.Economy,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CursorPagedResult<CityHouseholdAccountLedgerEntryView>>
            GetCityHouseholdAccountLedgerFeedAsync(
                Guid householdAccountId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
        {
            string url = BuildCursorFeedUrl(
                baseUrl: string.Format(
                    format: CityHouseholdAccountLedgerFeedEndpointTemplate,
                    arg0: householdAccountId),
                cursor: cursor,
                pageSize: pageSize);

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await
                response.ReadJsonOrThrowDownstreamAsync<CursorPagedResult<CityHouseholdAccountLedgerEntryView>>(
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

        private static string BuildCursorFeedUrl(
            string baseUrl,
            string? cursor,
            int pageSize)
        {
            string cursorPart = string.IsNullOrWhiteSpace(cursor)
                ? string.Empty
                : $"&cursor={Uri.EscapeDataString(cursor)}";

            return $"{baseUrl}?pageSize={pageSize}{cursorPart}";
        }
    }
}
