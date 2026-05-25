using System.Net;
using System.Text.Json;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Infrastructure.Tests.Http;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.SimulationSystems
{
    public sealed class CityRoadSegmentConditionsClientTests
    {
        [Fact]
        public async Task GetByCityIdAsync_WhenResponseIsSuccessful_ReturnsMappedSnapshot()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    HttpClientTestSupport.CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: new CityRoadSegmentConditionsView(
                            CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            EffectiveTickId: 42,
                            LastEvaluatedAtUtc: new DateTimeOffset(
                                year: 2048,
                                month: 2,
                                day: 3,
                                hour: 9,
                                minute: 0,
                                second: 0,
                                offset: TimeSpan.Zero),
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
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityRoadSegmentConditionsClient client =
                HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

            CityRoadSegmentConditionsSnapshot? result = await client.GetByCityIdAsync(
                cityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                cancellationToken: CancellationToken.None);

            HttpClientTestSupport.RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Get,
                actual: request.Method);
            Assert.Equal(
                expected:
                "https://localhost:7155/api/classic-city/cities/11111111-1111-1111-1111-111111111111/road-access/segments",
                actual: request.RequestUri);
            Assert.Null(request.Body);

            Assert.NotNull(result);
            Assert.Equal(
                expected: 42,
                actual: result.EffectiveTickId);
            Assert.Equal(
                expected: 0.87m,
                actual: result.RoadSupportIndex);
            CityRoadSegmentConditionSnapshot segment = Assert.Single(result.Segments);
            Assert.Equal(
                expected: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                actual: segment.RoadSegmentId);
            Assert.Equal(
                expected: "Main Artery",
                actual: segment.Name);
            Assert.Equal(
                expected: "arterial",
                actual: segment.Type);
            Assert.Equal(
                expected: 320.5m,
                actual: segment.LengthMeters);
            Assert.Equal(
                expected: 0.73m,
                actual: segment.SpeedMultiplierIndex);
        }

        [Fact]
        public async Task GetByCityIdAsync_WhenResponseIsNotSuccessful_ReturnsNull()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityRoadSegmentConditionsClient client =
                HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

            CityRoadSegmentConditionsSnapshot? result = await client.GetByCityIdAsync(
                cityId: Guid.NewGuid(),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByCityIdAsync_WhenResponseBodyIsEmpty_ReturnsNull()
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
            ICityRoadSegmentConditionsClient client =
                HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

            CityRoadSegmentConditionsSnapshot? result = await client.GetByCityIdAsync(
                cityId: Guid.NewGuid(),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByCityIdAsync_WhenResponseBodyIsMalformed_ThrowsJsonException()
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
            ICityRoadSegmentConditionsClient client =
                HttpClientTestSupport.CreateRoadSegmentConditionsClient(httpClient);

            await Assert.ThrowsAsync<JsonException>(() => client.GetByCityIdAsync(
                cityId: Guid.NewGuid(),
                cancellationToken: CancellationToken.None));
        }
    }
}
