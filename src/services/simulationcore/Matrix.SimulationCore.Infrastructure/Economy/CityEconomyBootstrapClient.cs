using System.Net.Http.Json;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;

namespace Matrix.SimulationCore.Infrastructure.Economy
{
    internal sealed class CityEconomyBootstrapClient(HttpClient client) : ICityEconomyBootstrapClient
    {
        private readonly HttpClient _client = client;

        public async Task<CityEconomyBootstrapResult> InitializeAsync(
            Guid cityId,
            string simulationKind,
            string economyProfile,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken)
        {
            string url = $"/api/economy/Budget/cities/{cityId}/bootstrap";

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                requestUri: url,
                value: new InitializeCityEconomyRequest(
                    SimulationKind: simulationKind,
                    EconomyProfile: economyProfile,
                    CreatedAtUtc: createdAtUtc),
                cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    message: $"Economy bootstrap request failed with status code {(int)response.StatusCode}.",
                    inner: null,
                    statusCode: response.StatusCode);

            CityEconomyBootstrapResultView? payload =
                await response.Content.ReadFromJsonAsync<CityEconomyBootstrapResultView>(
                    cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("Economy bootstrap response was empty.");

            return new CityEconomyBootstrapResult(
                UnitKind: payload.UnitKind,
                UnitCode: payload.UnitCode,
                UnitDisplayName: payload.UnitDisplayName,
                UnitSymbol: payload.UnitSymbol);
        }
    }
}
