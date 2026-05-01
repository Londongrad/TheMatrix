using System.Net;
using System.Text.Json;
using Matrix.SimulationCore.Infrastructure.Tests.Http;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.SimulationSystems;

public sealed class CityRoadSegmentConditionsClientTests
{
    [Fact]
    public async Task GetByCityIdAsync_WhenResponseIsSuccessful_ReturnsMappedSnapshot()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateJsonResponse(
                    HttpStatusCode.OK,
                    new CityRoadSegmentConditionsView(
                        CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        EffectiveTickId: 42,
                        LastEvaluatedAtUtc: new DateTimeOffset(2048, 2, 3, 9, 0, 0, TimeSpan.Zero),
                        RoadSupportIndex: 0.87m,
                        Segments:
                        [
                            new CityRoadSegmentConditionView(
                                RoadSegmentId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                                DistrictId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                                FromRoadNodeId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                                ToRoadNodeId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                                Name: "Main Artery",
                                Type: "arterial",
                                LengthMeters: 320.5m,
                                PassabilityIndex: 0.91m,
                                SpeedMultiplierIndex: 0.73m,
                                SlipRiskIndex: 0.11m,
                                ClosureRiskIndex: 0.04m,
                                MaintenancePriorityIndex: 0.66m)
                        ])))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

        var result = await client.GetByCityIdAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CancellationToken.None);

        HttpClientTestSupport.RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://localhost:7155/api/classic-city/cities/11111111-1111-1111-1111-111111111111/road-access/segments", request.RequestUri);
        Assert.Null(request.Body);

        Assert.NotNull(result);
        Assert.Equal(42, result.EffectiveTickId);
        Assert.Equal(0.87m, result.RoadSupportIndex);
        var segment = Assert.Single(result.Segments);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), segment.RoadSegmentId);
        Assert.Equal("Main Artery", segment.Name);
        Assert.Equal("arterial", segment.Type);
        Assert.Equal(320.5m, segment.LengthMeters);
        Assert.Equal(0.73m, segment.SpeedMultiplierIndex);
    }

    [Fact]
    public async Task GetByCityIdAsync_WhenResponseIsNotSuccessful_ReturnsNull()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

        var result = await client.GetByCityIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCityIdAsync_WhenResponseBodyIsEmpty_ReturnsNull()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateStringResponse(HttpStatusCode.OK, "null"))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

        var result = await client.GetByCityIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCityIdAsync_WhenResponseBodyIsMalformed_ThrowsJsonException()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateStringResponse(HttpStatusCode.OK, "{"))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(
            () => client.GetByCityIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
