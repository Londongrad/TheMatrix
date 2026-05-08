using System.Net;
using System.Text.Json;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Infrastructure.SimulationCore;
using Matrix.Population.Infrastructure.Tests.Http;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.Http.HttpClientTestSupport;

namespace Matrix.Population.Infrastructure.Tests.SimulationCore;

public sealed class CityActiveTripClientTests
{
    [Fact]
    public async Task ListActiveByCityAsync_WhenResponseIsSuccessful_MapsPayload()
    {
        Guid cityId = Guid.Parse("eb548b8e-a414-4ec2-95e5-758bdfed100a");
        Guid travellerId = Guid.Parse("d9a08772-f19a-4f7c-9b85-f3b863e5280b");
        Guid fromEntityId = Guid.Parse("814c92d1-5dd5-40cb-b2d6-77fb3e9ca255");
        Guid toEntityId = Guid.Parse("25d9efa9-c654-4106-a6f7-dca648c11cb5");
        CityActiveTripView[] payload =
        [
            new(
                TripId: Guid.Parse("f7bb40ee-48a5-4464-b10b-ca3c4955663e"),
                CityId: cityId,
                TravellerEntityId: travellerId,
                Subject: "Work commute",
                Purpose: "WorkCommute",
                Profile: "Pedestrian",
                Status: "InProgress",
                MovementCapabilityIndex: 0.8m,
                UsedDynamicRoadConditions: true,
                PlannedAtTickId: 10,
                ConditionsEffectiveTickId: 11,
                LastAdvancedTickId: 12,
                StartedAtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
                LastAdvancedAtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 5, 0, TimeSpan.Zero),
                ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 20, 0, TimeSpan.Zero),
                ArrivedAtSimTimeUtc: null,
                CurrentProgressIndex: 0.25m,
                TotalDistanceMeters: 1000m,
                DistanceTravelledMeters: 250m,
                RemainingDistanceMeters: 750m,
                PlannedTravelTimeMinutes: 20m,
                AdjustedTravelTimeMinutes: 24m,
                From: new CityActiveTripEndpointView(
                    Kind: "ResidentialBuilding",
                    EntityId: fromEntityId,
                    DistrictId: Guid.Parse("3548965a-a3d2-4a09-8438-5dba11b138dd"),
                    RoadNodeId: Guid.Parse("0835e7b4-8b34-4623-9345-08661fab9e24"),
                    Name: "Home",
                    PositionX: 10m,
                    PositionY: 20m),
                To: new CityActiveTripEndpointView(
                    Kind: "CityAnchor",
                    EntityId: toEntityId,
                    DistrictId: Guid.Parse("d1860c80-87ab-4d78-ae18-cf94a09d04c6"),
                    RoadNodeId: Guid.Parse("9c4f2460-787a-49ea-b829-844ae84ec64e"),
                    Name: "Office",
                    PositionX: 30m,
                    PositionY: 40m),
                Current: new CityActiveTripProgressView(
                    DistrictId: Guid.Parse("3548965a-a3d2-4a09-8438-5dba11b138dd"),
                    RoadSegmentId: Guid.Parse("07d3f3d9-8861-4d50-8538-d44ffbf9494d"),
                    SegmentProgressIndex: 0.4m,
                    PositionX: 12m,
                    PositionY: 21m))
        ];
        HttpClient client = CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"/api/cities/{cityId}/trips/active", request.RequestUri!.PathAndQuery);
            return Task.FromResult(JsonResponse(JsonSerializer.Serialize(payload)));
        });
        var tripClient = new CityActiveTripClient(client);

        IReadOnlyCollection<CityPopulationActiveTripSnapshot> result = await tripClient.ListActiveByCityAsync(
            cityId,
            CancellationToken.None);

        CityPopulationActiveTripSnapshot item = Assert.Single(result);
        Assert.Equal(travellerId, item.TravellerEntityId);
        Assert.Equal("Work commute", item.Subject);
        Assert.Equal("Home", item.FromName);
        Assert.Equal(fromEntityId, item.FromEntityId);
        Assert.Equal("Office", item.ToName);
        Assert.Equal(toEntityId, item.ToEntityId);
    }

    [Fact]
    public async Task FindActiveByTravellerAsync_WhenResponseContainsTraveller_ReturnsMatchingTrip()
    {
        Guid cityId = Guid.Parse("b0201577-f95e-4923-b67d-c66efc4ecc3b");
        Guid travellerId = Guid.Parse("f9620f8d-a848-48d9-b2d2-cba90f67345c");
        CityActiveTripView[] payload =
        [
            CreateTripView(Guid.Parse("4b7919ee-d7e5-4c17-a073-f3917853f588")),
            CreateTripView(travellerId)
        ];
        HttpClient client = CreateClient((_, _) =>
            Task.FromResult(JsonResponse(JsonSerializer.Serialize(payload))));
        var tripClient = new CityActiveTripClient(client);

        CityPopulationActiveTripSnapshot? result = await tripClient.FindActiveByTravellerAsync(
            cityId,
            travellerId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(travellerId, result.TravellerEntityId);
    }

    [Fact]
    public async Task TryDispatchAsync_PostsExpectedPayloadAndReturnsSuccessFlag()
    {
        string? requestJson = null;
        Guid cityId = Guid.Parse("4f2c59e7-6b92-4085-b33e-1b13b89827d8");
        HttpClient client = CreateClient(async (request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/api/cities/{cityId}/trips", request.RequestUri!.PathAndQuery);
            requestJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var tripClient = new CityActiveTripClient(client);

        bool dispatched = await tripClient.TryDispatchAsync(
            new CityPopulationTripDispatchRequest(
                CityId: cityId,
                FromKind: "ResidentialBuilding",
                FromId: Guid.Parse("efe0c92b-fd88-4871-ab90-e0d28f4f4966"),
                ToKind: "CityAnchor",
                ToId: Guid.Parse("b580d7cd-df59-476a-aa49-fecdc61c06d8"),
                Purpose: "WorkCommute",
                Profile: "Pedestrian",
                MovementCapabilityIndex: 0.9m,
                TravellerEntityId: Guid.Parse("2dc65c44-e0de-493b-a366-c028d6da52e5"),
                Subject: "Morning route"),
            CancellationToken.None);

        Assert.True(dispatched);
        Assert.NotNull(requestJson);
        Assert.Contains("\"kind\":\"ResidentialBuilding\"", requestJson);
        Assert.Contains("\"kind\":\"CityAnchor\"", requestJson);
        Assert.Contains("\"movementCapabilityIndex\":0.9", requestJson);
        Assert.Contains("\"subject\":\"Morning route\"", requestJson);
    }

    private static CityActiveTripView CreateTripView(Guid travellerId)
    {
        return new CityActiveTripView(
            TripId: Guid.NewGuid(),
            CityId: Guid.Parse("b0201577-f95e-4923-b67d-c66efc4ecc3b"),
            TravellerEntityId: travellerId,
            Subject: "Trip",
            Purpose: "WorkCommute",
            Profile: "Pedestrian",
            Status: "InProgress",
            MovementCapabilityIndex: 1m,
            UsedDynamicRoadConditions: false,
            PlannedAtTickId: 1,
            ConditionsEffectiveTickId: null,
            LastAdvancedTickId: 1,
            StartedAtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            LastAdvancedAtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 30, 0, TimeSpan.Zero),
            ArrivedAtSimTimeUtc: null,
            CurrentProgressIndex: 0.1m,
            TotalDistanceMeters: 100m,
            DistanceTravelledMeters: 10m,
            RemainingDistanceMeters: 90m,
            PlannedTravelTimeMinutes: 10m,
            AdjustedTravelTimeMinutes: 10m,
            From: new CityActiveTripEndpointView(
                Kind: "ResidentialBuilding",
                EntityId: Guid.Parse("7d1dd3b0-0957-4d10-a02b-ebd09f613f74"),
                DistrictId: Guid.Parse("5ef9209e-b4ff-4fd2-ac9d-d662ad7e2903"),
                RoadNodeId: Guid.Parse("f1b9744a-f979-4b4c-8bcc-d2d07e0ce39c"),
                Name: "From",
                PositionX: 0m,
                PositionY: 0m),
            To: new CityActiveTripEndpointView(
                Kind: "CityAnchor",
                EntityId: Guid.Parse("4cb551c2-17c9-44be-baf3-4de117018b5f"),
                DistrictId: Guid.Parse("4bdefe73-87c1-4bd4-875c-f809fc9fe0b6"),
                RoadNodeId: Guid.Parse("e7f83f45-4da0-40a7-a24c-dc67efca1c7f"),
                Name: "To",
                PositionX: 1m,
                PositionY: 1m),
            Current: new CityActiveTripProgressView(
                DistrictId: Guid.Parse("5ef9209e-b4ff-4fd2-ac9d-d662ad7e2903"),
                RoadSegmentId: null,
                SegmentProgressIndex: 0.1m,
                PositionX: 0.1m,
                PositionY: 0.1m));
    }
}
