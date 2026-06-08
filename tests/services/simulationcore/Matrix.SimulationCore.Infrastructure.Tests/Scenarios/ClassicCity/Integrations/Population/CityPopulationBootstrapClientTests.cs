using System.Net;
using System.Text.Json;
using Matrix.Population.Contracts.Scenarios.ClassicCity;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Infrastructure.Tests.Http;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Scenarios.ClassicCity.Integrations.Population
{
    public sealed class CityPopulationBootstrapClientTests
    {
        [Fact]
        public async Task InitializeAsync_WhenResponseIsSuccessful_ReturnsMappedSummary()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    HttpClientTestSupport.CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: new CityPopulationBootstrapSummaryDto(
                            CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                            RequestedPeopleCount: 1200,
                            GeneratedPeopleCount: 1190,
                            HouseholdCount: 480,
                            HousedHouseholdCount: 470,
                            HomelessHouseholdCount: 10,
                            HousedPeopleCount: 1170,
                            HomelessPeopleCount: 20)))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityPopulationBootstrapClient client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);
            CityPopulationBootstrapInitializationRequest request = CreateRequest();

            CityPopulationBootstrapSummary result = await client.InitializeAsync(
                request: request,
                cancellationToken: CancellationToken.None);

            HttpClientTestSupport.RecordedRequest recordedRequest = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: recordedRequest.Method);
            Assert.Equal(
                expected: "https://localhost:7155" + ClassicCityPopulationApiRoutes.InitializePath,
                actual: recordedRequest.RequestUri);
            Assert.Equal(
                expected: "application/json",
                actual: recordedRequest.ContentType);

            using var json = JsonDocument.Parse(recordedRequest.Body!);
            Assert.Equal(
                expected: request.CityId,
                actual: json.RootElement.GetProperty("cityId")
                   .GetGuid());
            Assert.Equal(
                expected: request.CurrentDate.ToString("yyyy-MM-dd"),
                actual: json.RootElement.GetProperty("currentDate")
                   .GetString());
            Assert.Equal(
                expected: request.CreatedAtUtc,
                actual: json.RootElement.GetProperty("createdAtUtc")
                   .GetDateTimeOffset());
            Assert.Equal(
                expected: request.PeopleCount,
                actual: json.RootElement.GetProperty("peopleCount")
                   .GetInt32());
            Assert.Equal(
                expected: request.RandomSeed,
                actual: json.RootElement.GetProperty("randomSeed")
                   .GetInt32());
            Assert.Equal(
                expected: request.Environment.ClimateZone,
                actual: json.RootElement.GetProperty("environment")
                   .GetProperty("climateZone")
                   .GetString());
            Assert.Equal(
                expected: request.Environment.Hemisphere,
                actual: json.RootElement.GetProperty("environment")
                   .GetProperty("hemisphere")
                   .GetString());
            Assert.Equal(
                expected: request.Environment.UtcOffsetMinutes,
                actual: json.RootElement.GetProperty("environment")
                   .GetProperty("utcOffsetMinutes")
                   .GetInt32());
            Assert.Equal(
                expected: request.Tuning.HousingPressurePercent,
                actual: json.RootElement.GetProperty("tuning")
                   .GetProperty("housingPressurePercent")
                   .GetInt32());
            Assert.Equal(
                expected: request.CityAnchors.Count,
                actual: json.RootElement.GetProperty("cityAnchors")
                   .GetArrayLength());
            Assert.Equal(
                expected: request.ResidentialBuildings.Count,
                actual: json.RootElement.GetProperty("residentialBuildings")
                   .GetArrayLength());

            Assert.Equal(
                expected: request.CityId,
                actual: result.CityId);
            Assert.Equal(
                expected: 1200,
                actual: result.RequestedPeopleCount);
            Assert.Equal(
                expected: 1190,
                actual: result.GeneratedPeopleCount);
            Assert.Equal(
                expected: 480,
                actual: result.HouseholdCount);
            Assert.Equal(
                expected: 470,
                actual: result.HousedHouseholdCount);
            Assert.Equal(
                expected: 10,
                actual: result.HomelessHouseholdCount);
            Assert.Equal(
                expected: 1170,
                actual: result.HousedPeopleCount);
            Assert.Equal(
                expected: 20,
                actual: result.HomelessPeopleCount);
        }

        [Fact]
        public async Task InitializeAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
        {
            var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
            };
            using HttpClient httpClient = HttpClientTestSupport.CreateHttpClient(handler);
            ICityPopulationBootstrapClient client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);

            HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(()
                => client.InitializeAsync(
                    request: CreateRequest(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.ServiceUnavailable,
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
            ICityPopulationBootstrapClient client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => client.InitializeAsync(
                    request: CreateRequest(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population bootstrap response was empty.",
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
            ICityPopulationBootstrapClient client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);

            await Assert.ThrowsAsync<JsonException>(() => client.InitializeAsync(
                request: CreateRequest(),
                cancellationToken: CancellationToken.None));
        }

        private static CityPopulationBootstrapInitializationRequest CreateRequest()
        {
            return new CityPopulationBootstrapInitializationRequest(
                CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 2,
                    day: 3),
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero),
                PeopleCount: 1200,
                RandomSeed: 4242,
                Environment: new CityPopulationBootstrapEnvironment(
                    ClimateZone: "Temperate",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 180),
                Tuning: new CityPopulationBootstrapTuning(
                    HousingPressurePercent: 35,
                    EconomicStabilityPercent: 62,
                    SocialVolatilityPercent: 18,
                    FamilyFormationPercent: 44),
                CityAnchors:
                [
                    new CityAnchorSeed(
                        CityAnchorId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        DistrictId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        AccessRoadNodeId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                        Name: "Central Hospital",
                        Type: "Hospital",
                        Capacity: 400,
                        PositionX: 12.34m,
                        PositionY: 56.78m,
                        CreatedAtUtc: new DateTimeOffset(
                            year: 2048,
                            month: 2,
                            day: 3,
                            hour: 4,
                            minute: 10,
                            second: 0,
                            offset: TimeSpan.Zero))
                ],
                ResidentialBuildings:
                [
                    new ResidentialBuildingSeed(
                        ResidentialBuildingId: Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        DistrictId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        ResidentCapacity: 240)
                ]);
        }
    }
}
