namespace Matrix.ApiGateway.DownstreamClients.Population
{
    public sealed class PopulationApiClient(HttpClient client, ILogger<PopulationApiClient> logger) 
        : IPopulationApiClient
    {
        private readonly HttpClient _client = client;
        private readonly ILogger<PopulationApiClient> _logger = logger;

        private const string IncomeEndpointTemplate = "/api/population/Simulation/income/month/{0}";
        private const string HealthEndpoint = "/api/population/Simulation/health";
        private const string InitializeEndpoint = "/api/population/Simulation/init";

        public async Task TriggerMonthlyIncomeAsync(int month, CancellationToken cancellationToken = default)
        {
            // ƒолжен быть POST, потому что в Population.Api стоит [HttpPost(...)]
            var relativeUrl = string.Format(IncomeEndpointTemplate, month);

            // “ело нам не нужно Ц просто команда без payload
            using var response = await _client.PostAsync(relativeUrl, content: null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to trigger monthly income for month {Month}. Status code: {StatusCode}",
                    month,
                    response.StatusCode);

                response.EnsureSuccessStatusCode(); // бросит исключение
            }
        }

        public async Task InitializePopulationAsync(
            int peopleCount,
            int? randomSeed = null,
            CancellationToken cancellationToken = default)
        {
            // —обираем querystring руками, чтобы без зависимостей
            var query = $"?peopleCount={peopleCount}";
            if (randomSeed.HasValue)
            {
                query += $"&randomSeed={randomSeed.Value}";
            }

            var url = InitializeEndpoint + query;

            using var response = await _client.PostAsync(url, content: null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to initialize population (peopleCount={PeopleCount}, seed={Seed}). Status code: {StatusCode}",
                    peopleCount,
                    randomSeed,
                    response.StatusCode);

                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _client.GetAsync(HealthEndpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Population health check failed with status code {StatusCode}",
                    response.StatusCode);

                return false;
            }

            // ћожно даже попробовать прочитать тело, если захочетс€
            // var json = await response.Content.ReadFromJsonAsync<HealthDto>(cancellationToken: cancellationToken);

            return true;
        }
    }
}