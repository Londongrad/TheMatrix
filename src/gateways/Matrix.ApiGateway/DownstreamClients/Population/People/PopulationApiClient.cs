using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population.People
{
    public sealed class PopulationApiClient(HttpClient client)
        : IPopulationApiClient
    {
        #region [ Fields ]

        private readonly HttpClient _client = client;

        #endregion [ Fields ]

        #region [ Methods ]

        public async Task InitializePopulationAsync(
            int peopleCount,
            int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            // Собираем querystring руками, чтобы без зависимостей
            string query = $"?peopleCount={peopleCount}";

            if (randomSeed.HasValue)
                query += $"&randomSeed={randomSeed.Value}";

            string url = InitializeEndpoint + query;

            using HttpResponseMessage response =
                await _client.PostAsync(
                    requestUri: url,
                    content: null,
                    cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
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

            // Если вдруг API вернёт пустое тело — это уже баг, не бизнес-кейс
            return result ?? throw new InvalidOperationException("Empty response from Population API.");
        }

        #endregion [ Methods ]

        #region [ Constants ]

        private const string ServiceName = "Population";

        private const string PopulationBaseEndpoint = "/api/population";

        private const string InitializeEndpoint = PopulationBaseEndpoint + "/init";
        private const string GetPagedEndpoint = PopulationBaseEndpoint + "/citizens";

        #endregion [ Constants ]
    }
}
