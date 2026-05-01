using System.Net;
using System.Text.Json;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Provisioning.Abstractions;
using Matrix.SimulationCore.Infrastructure.Tests.Http;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Population;

public sealed class CityPopulationBootstrapClientTests
{
    [Fact]
    public async Task InitializeAsync_WhenResponseIsSuccessful_ReturnsMappedSummary()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(
                HttpClientTestSupport.CreateJsonResponse(
                    HttpStatusCode.OK,
                    new CityPopulationBootstrapSummaryDto(
                        CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        RequestedPeopleCount: 1200,
                        GeneratedPeopleCount: 1190,
                        HouseholdCount: 480,
                        HousedHouseholdCount: 470,
                        HomelessHouseholdCount: 10,
                        HousedPeopleCount: 1170,
                        HomelessPeopleCount: 20)))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);
        CityPopulationBootstrapInitializationRequest request = CreateRequest();

        CityPopulationBootstrapSummary result = await client.InitializeAsync(request, CancellationToken.None);

        HttpClientTestSupport.RecordedRequest recordedRequest = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, recordedRequest.Method);
        Assert.Equal("https://localhost:7155/api/population/init", recordedRequest.RequestUri);
        Assert.Equal("application/json", recordedRequest.ContentType);

        using JsonDocument json = JsonDocument.Parse(recordedRequest.Body!);
        Assert.Equal(request.CityId, json.RootElement.GetProperty("cityId").GetGuid());
        Assert.Equal(request.CurrentDate.ToString("yyyy-MM-dd"), json.RootElement.GetProperty("currentDate").GetString());
        Assert.Equal(request.CreatedAtUtc, json.RootElement.GetProperty("createdAtUtc").GetDateTimeOffset());
        Assert.Equal(request.PeopleCount, json.RootElement.GetProperty("peopleCount").GetInt32());
        Assert.Equal(request.RandomSeed, json.RootElement.GetProperty("randomSeed").GetInt32());
        Assert.Equal(request.Environment.ClimateZone, json.RootElement.GetProperty("environment").GetProperty("climateZone").GetString());
        Assert.Equal(request.Environment.Hemisphere, json.RootElement.GetProperty("environment").GetProperty("hemisphere").GetString());
        Assert.Equal(request.Environment.UtcOffsetMinutes, json.RootElement.GetProperty("environment").GetProperty("utcOffsetMinutes").GetInt32());
        Assert.Equal(request.Tuning.HousingPressurePercent, json.RootElement.GetProperty("tuning").GetProperty("housingPressurePercent").GetInt32());
        Assert.Equal(request.CityAnchors.Count, json.RootElement.GetProperty("cityAnchors").GetArrayLength());
        Assert.Equal(request.ResidentialBuildings.Count, json.RootElement.GetProperty("residentialBuildings").GetArrayLength());

        Assert.Equal(request.CityId, result.CityId);
        Assert.Equal(1200, result.RequestedPeopleCount);
        Assert.Equal(1190, result.GeneratedPeopleCount);
        Assert.Equal(480, result.HouseholdCount);
        Assert.Equal(470, result.HousedHouseholdCount);
        Assert.Equal(10, result.HomelessHouseholdCount);
        Assert.Equal(1170, result.HousedPeopleCount);
        Assert.Equal(20, result.HomelessPeopleCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenResponseIsNotSuccessful_ThrowsHttpRequestException()
    {
        var handler = new HttpClientTestSupport.RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        };
        using var httpClient = HttpClientTestSupport.CreateHttpClient(handler);
        var client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);

        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.InitializeAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
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
        var client = HttpClientTestSupport.CreatePopulationBootstrapClient(httpClient);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.InitializeAsync(CreateRequest(), CancellationToken.None));

        Assert.Equal("Population bootstrap response was empty.", exception.Message);
    }

    private static CityPopulationBootstrapInitializationRequest CreateRequest()
    {
        return new CityPopulationBootstrapInitializationRequest(
            CityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CurrentDate: new DateOnly(2048, 2, 3),
            CreatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero),
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
                    CreatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 10, 0, TimeSpan.Zero))
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
