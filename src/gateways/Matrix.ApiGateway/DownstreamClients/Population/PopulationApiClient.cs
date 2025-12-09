using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population
{
    public sealed class PopulationApiClient(HttpClient client)
        : IPopulationApiClient
    {
        #region [ Fields ]

        private readonly HttpClient _client = client;

        #endregion [ Fields ]

        public async Task InitializePopulationAsync(
            int peopleCount,
            int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            // Собираем querystring руками, чтобы без зависимостей
            string query = $"?peopleCount={peopleCount}";
            if (randomSeed.HasValue) query += $"&randomSeed={randomSeed.Value}";

            string url = InitializeEndpoint + query;

            using HttpResponseMessage response =
                await _client.PostAsync(requestUri: url, content: null, cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<PersonDto> KillPersonAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            string url = $"/api/population/{id}/kill";

            using HttpResponseMessage response =
                await _client.PostAsync(requestUri: url, content: null, cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();

            PersonDto? dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for KillPerson.");
        }

        public async Task<PersonDto> ResurrectPersonAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            string url = $"/api/population/{id}/resurrect";

            using HttpResponseMessage response =
                await _client.PostAsync(requestUri: url, content: null, cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();

            PersonDto? dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ??
                   throw new InvalidOperationException("Population API returned empty body for ResurrectPerson.");
        }

        public async Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            string query = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            string url = GetPagedEndpoint + query;

            using HttpResponseMessage response =
                await _client.GetAsync(requestUri: url, cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();

            PagedResult<PersonDto>? result = await response.Content
                .ReadFromJsonAsync<PagedResult<PersonDto>>(cancellationToken: cancellationToken);

            // Если вдруг API вернёт пустое тело — это уже баг, не бизнес-кейс
            return result ?? throw new InvalidOperationException("Empty response from Population API.");
        }

        public async Task<PersonDto> UpdatePersonAsync(Guid id, UpdatePersonRequest request,
            CancellationToken cancellationToken = default)
        {
            string url = $"/api/population/citizens/{id}";

            using HttpResponseMessage response = await _client.PutAsJsonAsync(requestUri: url, value: request,
                cancellationToken: cancellationToken);
            response.EnsureSuccessStatusCode();

            PersonDto? dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for KillPerson.");
        }

        #region [ Constants ]

        private const string PopulationBaseEndpoint = "/api/population";

        private const string InitializeEndpoint = PopulationBaseEndpoint + "init";
        private const string GetPagedEndpoint = PopulationBaseEndpoint + "citizens";

        #endregion [ Constants ]
    }
}
