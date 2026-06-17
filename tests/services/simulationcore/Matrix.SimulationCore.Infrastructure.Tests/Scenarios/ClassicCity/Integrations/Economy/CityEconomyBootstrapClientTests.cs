using System.Net;
using System.Text.Json;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Infrastructure.Tests.Http;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Scenarios.ClassicCity.Integrations.Economy
{
    public sealed class CityEconomyBootstrapClientTests
    {
        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task InitializeAsync_WhenResponseIsSuccessful_ReturnsMappedResult()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    HttpClientTestSupport.CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: new CityEconomyBootstrapResultView(
                            CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            BudgetCreated: true,
                            CreatedAllocations: 3,
                            CreatedBusinesses: 7,
                            UnitKind: "Currency",
                            UnitCode: "CR",
                            UnitDisplayName: "Credits",
                            UnitSymbol: "₡")))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityEconomyBootstrapClient client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CityEconomyBootstrapResult result = await client.InitializeAsync(
                cityId: cityId,
                economyProfile: "Balanced",
                createdAtUtc: CreatedAtUtc,
                cancellationToken: CancellationToken.None);

            HttpClientTestSupport.RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.Equal(
                expected:
                "https://localhost:7155/api/economy/Budget/cities/11111111-1111-1111-1111-111111111111/bootstrap",
                actual: request.RequestUri);
            Assert.Equal(
                expected: "application/json",
                actual: request.ContentType);

            using var json = JsonDocument.Parse(request.Body!);
            Assert.Equal(
                expected: "classic-city",
                actual: json.RootElement.GetProperty("scenarioKey")
                   .GetString());
            Assert.Equal(
                expected: "Balanced",
                actual: json.RootElement.GetProperty("economyProfile")
                   .GetString());
            Assert.Equal(
                expected: CreatedAtUtc,
                actual: json.RootElement.GetProperty("createdAtUtc")
                   .GetDateTimeOffset());

            Assert.Equal(
                expected: "Currency",
                actual: result.UnitKind);
            Assert.Equal(
                expected: "CR",
                actual: result.UnitCode);
            Assert.Equal(
                expected: "Credits",
                actual: result.UnitDisplayName);
            Assert.Equal(
                expected: "₡",
                actual: result.UnitSymbol);
        }

        [Fact]
        public async Task InitializeAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityEconomyBootstrapClient client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);

            HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(()
                => client.InitializeAsync(
                    cityId: Guid.NewGuid(),
                    economyProfile: "Balanced",
                    createdAtUtc: CreatedAtUtc,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.BadGateway,
                actual: exception.StatusCode);
        }

        [Fact]
        public async Task InitializeAsync_WhenResponseBodyIsEmpty_ThrowsInvalidOperationException()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    HttpClientTestSupport.CreateStringResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: "null"))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityEconomyBootstrapClient client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => client.InitializeAsync(
                    cityId: Guid.NewGuid(),
                    economyProfile: "Balanced",
                    createdAtUtc: CreatedAtUtc,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Economy bootstrap response was empty.",
                actual: exception.Message);
        }

        [Fact]
        public async Task InitializeAsync_WhenResponseBodyIsMalformed_ThrowsJsonException()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    HttpClientTestSupport.CreateStringResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: "{"))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityEconomyBootstrapClient client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);

            await Assert.ThrowsAsync<JsonException>(() => client.InitializeAsync(
                cityId: Guid.NewGuid(),
                economyProfile: "Balanced",
                createdAtUtc: CreatedAtUtc,
                cancellationToken: CancellationToken.None));
        }
    }
}
