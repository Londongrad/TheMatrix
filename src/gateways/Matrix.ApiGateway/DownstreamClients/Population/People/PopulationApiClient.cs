using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;
using System.Globalization;

namespace Matrix.ApiGateway.DownstreamClients.Population.People
{
    public sealed class PopulationApiClient(HttpClient client)
        : IPopulationApiClient
    {
        #region [ Fields ]

        private readonly HttpClient _client = client;

        #endregion [ Fields ]

        #region [ Methods ]

        public async Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(
            InitializeCityPopulationRequest request,
            CancellationToken cancellationToken = default)
        {
            const string url = InitializeEndpoint;

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityPopulationBootstrapSummaryDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(
            Guid cityId,
            DateOnly currentDate,
            CancellationToken cancellationToken = default)
        {
            string currentDateValue = Uri.EscapeDataString(
                stringToEscape: currentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            string url = $"{PopulationBaseEndpoint}/cities/{cityId}/summary?currentDate={currentDateValue}";

            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<CityPopulationSummaryDto>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            string query = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            string url = GetPagedEndpoint + query;

            using HttpResponseMessage response =
                await _client.GetAsync(
                    requestUri: url,
                    cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);

            PagedResult<PersonDto>? result = await response.Content
               .ReadFromJsonAsync<PagedResult<PersonDto>>(cancellationToken: cancellationToken);

            return result ?? throw new InvalidOperationException("Empty response from Population API.");
        }

        #endregion [ Methods ]

        #region [ Constants ]

        private const string ServiceName = DownstreamServiceNames.Population;

        private const string PopulationBaseEndpoint = "/api/population";

        private const string InitializeEndpoint = PopulationBaseEndpoint + "/init";
        private const string GetPagedEndpoint = PopulationBaseEndpoint + "/citizens";

        #endregion [ Constants ]
    }
}
