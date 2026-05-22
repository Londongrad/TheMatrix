using System.Net;
using System.Text.Json;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.SimulationCore.Infrastructure.Tests.Http;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Economy;

public sealed class CityEconomyBootstrapClientTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 2, 3, 4, 5, 6, TimeSpan.Zero);

    [Fact]
    public async Task InitializeAsync_WhenResponseIsSuccessful_ReturnsMappedResult()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateJsonResponse(
                    HttpStatusCode.OK,
                    new CityEconomyBootstrapResultView(
                        CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        BudgetCreated: true,
                        CreatedAllocations: 3,
                        CreatedBusinesses: 7,
                        UnitKind: "Currency",
                        UnitCode: "CR",
                        UnitDisplayName: "Credits",
                        UnitSymbol: "₡")))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = await client.InitializeAsync(
            cityId: cityId,
            simulationKind: "ClassicCity",
            economyProfile: "Balanced",
            createdAtUtc: CreatedAtUtc,
            cancellationToken: CancellationToken.None);

        HttpClientTestSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://localhost:7155/api/economy/Budget/cities/11111111-1111-1111-1111-111111111111/bootstrap", request.RequestUri);
        Assert.Equal("application/json", request.ContentType);

        using JsonDocument json = JsonDocument.Parse(request.Body!);
        Assert.Equal("ClassicCity", json.RootElement.GetProperty("simulationKind").GetString());
        Assert.Equal("Balanced", json.RootElement.GetProperty("economyProfile").GetString());
        Assert.Equal(CreatedAtUtc, json.RootElement.GetProperty("createdAtUtc").GetDateTimeOffset());

        Assert.Equal("Currency", result.UnitKind);
        Assert.Equal("CR", result.UnitCode);
        Assert.Equal("Credits", result.UnitDisplayName);
        Assert.Equal("₡", result.UnitSymbol);
    }

    [Fact]
    public async Task InitializeAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.InitializeAsync(
                cityId: Guid.NewGuid(),
                simulationKind: "ClassicCity",
                economyProfile: "Balanced",
                createdAtUtc: CreatedAtUtc,
                cancellationToken: CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
    }

    [Fact]
    public async Task InitializeAsync_WhenResponseBodyIsEmpty_ThrowsInvalidOperationException()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateStringResponse(HttpStatusCode.OK, "null"))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.InitializeAsync(
                cityId: Guid.NewGuid(),
                simulationKind: "ClassicCity",
                economyProfile: "Balanced",
                createdAtUtc: CreatedAtUtc,
                cancellationToken: CancellationToken.None));

        Assert.Equal("Economy bootstrap response was empty.", exception.Message);
    }

    [Fact]
    public async Task InitializeAsync_WhenResponseBodyIsMalformed_ThrowsJsonException()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateStringResponse(HttpStatusCode.OK, "{"))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateEconomyBootstrapClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(
            () => client.InitializeAsync(
                cityId: Guid.NewGuid(),
                simulationKind: "ClassicCity",
                economyProfile: "Balanced",
                createdAtUtc: CreatedAtUtc,
                cancellationToken: CancellationToken.None));
    }
}
