using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Contracts.Models;

namespace Matrix.ApiGateway.DownstreamClients.Population
{
    public sealed class PopulationApiClient(HttpClient client)
        : IPopulationApiClient
    {
        private readonly HttpClient _client = client;

        private const string HealthEndpoint = "/api/population/health";
        private const string InitializeEndpoint = "/api/population/init";
        private const string GetPagedEndpoint = "/api/population/citizens";

        public async Task InitializePopulationAsync(
            int peopleCount,
            int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            // Собираем querystring руками, чтобы без зависимостей
            var query = $"?peopleCount={peopleCount}";
            if (randomSeed.HasValue)
            {
                query += $"&randomSeed={randomSeed.Value}";
            }

            var url = InitializeEndpoint + query;

            using var response = await _client.PostAsync(url, content: null, cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _client.GetAsync(HealthEndpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        public async Task<PersonDto> KillPersonAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var url = $"/api/population/{id}/kill";

            using var response = await _client.PostAsync(url, content: null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for KillPerson.");
        }

        public async Task<PersonDto> ResurrectPersonAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var url = $"/api/population/{id}/resurrect";

            using var response = await _client.PostAsync(url, content: null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for ResurrectPerson.");
        }

        public async Task<PagedResult<PersonDto>> GetCitizensPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            var url = GetPagedEndpoint + query;

            using var response = await _client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<PagedResult<PersonDto>>(cancellationToken: cancellationToken);

            // Если вдруг API вернёт пустое тело — это уже баг, не бизнес-кейс
            return result ?? throw new InvalidOperationException("Empty response from Population API.");
        }

        public async Task<PersonDto> UpdatePersonAsync(Guid id, UpdatePersonRequest request, CancellationToken cancellationToken = default)
        {
            var url = $"/api/population/citizens/{id}";

            using var response = await _client.PutAsJsonAsync(url, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<PersonDto>(cancellationToken: cancellationToken);
            return dto ?? throw new InvalidOperationException("Population API returned empty body for KillPerson.");
        }
    }
}