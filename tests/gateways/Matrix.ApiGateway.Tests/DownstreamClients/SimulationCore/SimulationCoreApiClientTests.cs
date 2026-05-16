using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Matrix.SimulationCore.Contracts.Simulation.Requests;
using Matrix.SimulationCore.Contracts.Simulation.Views;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.SimulationCore;

public sealed class SimulationCoreApiClientTests
{
    [Fact]
    public async Task SimulationApiClientGetClockAsync_WhenResponseIsSuccessful_ReturnsClockAndUsesExpectedUrl()
    {
        Guid simulationId = Guid.Parse("bcd7b39c-e6e0-46ab-bf80-f6b7c56d4c6b");
        SimulationClockView clock = CreateSimulationClockView(simulationId: simulationId);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, clock))
        };
        ISimulationApiClient client = CreateSimulationApiClient(CreateHttpClient(handler));

        SimulationClockView result = await client.GetClockAsync(simulationId, CancellationToken.None);

        Assert.Equal(clock, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith($"/api/simulations/{simulationId}", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SimulationApiClientSetClockSpeedAsync_WhenCalled_PostsJsonPayload()
    {
        Guid simulationId = Guid.Parse("3de2969c-ab62-4539-8cc3-bfe8853b71d8");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NoContent))
        };
        ISimulationApiClient client = CreateSimulationApiClient(CreateHttpClient(handler));

        await client.SetClockSpeedAsync(
            simulationId: simulationId,
            request: new SetSpeedRequest(2.5m),
            cancellationToken: CancellationToken.None);

        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith($"/api/simulations/{simulationId}/speed", request.RequestUri, StringComparison.Ordinal);
        Assert.Equal("application/json", request.ContentType);
        Assert.Contains("\"multiplier\":2.5", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SimulationApiClientPauseClockAsync_WhenDownstreamFails_ThrowsDownstreamServiceException()
    {
        Guid simulationId = Guid.Parse("8d8dbfc3-08a1-48d1-b4f8-6f9845e8c93e");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.BadGateway, "{\"error\":\"pause-failed\"}"))
        };
        ISimulationApiClient client = CreateSimulationApiClient(CreateHttpClient(handler));

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => client.PauseClockAsync(simulationId, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Contains("pause-failed", exception.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CitiesApiClientListCitiesAsync_WhenCalled_UsesLowercaseIncludeArchivedQuery()
    {
        var cities = new[]
        {
            new CityListItemView(
                CityId: Guid.Parse("8a3894d0-6989-4259-bbb4-9cfbd8c8b95c"),
                SimulationId: Guid.Parse("52df4d3c-3aa6-43ba-94f1-3c51350b7f9b"),
                Name: "Novy Mir",
                SimulationKind: "ClassicCity",
                Status: "Active",
                CreatedAtUtc: new DateTimeOffset(2048, 6, 1, 8, 0, 0, TimeSpan.Zero),
                PopulationBootstrapCompletedAtUtc: null,
                PopulationBootstrapFailedAtUtc: null,
                PopulationBootstrapFailureCode: null,
                ArchivedAtUtc: null)
        };
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse<IReadOnlyList<CityListItemView>>(HttpStatusCode.OK, cities))
        };
        ICitiesApiClient client = CreateCitiesApiClient(CreateHttpClient(handler));

        IReadOnlyList<CityListItemView> result = await client.ListCitiesAsync(includeArchived: true, cancellationToken: CancellationToken.None);

        Assert.Equal(cities, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith("/api/cities?includeArchived=true", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CitiesApiClientGetResidentialBuildingsAsync_WhenDistrictIsProvided_UsesDistrictQuery()
    {
        Guid cityId = Guid.Parse("29f7358c-5207-44db-a915-85a77c357404");
        Guid districtId = Guid.Parse("0fca63f9-8eea-4cac-b17d-64be4bdfb0df");
        ResidentialBuildingView[] buildings =
        [
            new ResidentialBuildingView(
                ResidentialBuildingId: Guid.Parse("5d831941-81f7-4ecf-a370-225b1aefa3ab"),
                CityId: cityId,
                DistrictId: districtId,
                AccessRoadNodeId: Guid.Parse("79b57b7b-4d8d-4c40-8e70-340fe2a62088"),
                Name: "Sector-1 Block-A",
                Type: "MidRise",
                ResidentCapacity: 120,
                PositionX: 14.2m,
                PositionY: 8.5m,
                CreatedAtUtc: new DateTimeOffset(2048, 6, 1, 8, 0, 0, TimeSpan.Zero))
        ];
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse<IReadOnlyList<ResidentialBuildingView>>(HttpStatusCode.OK, buildings))
        };
        ICitiesApiClient client = CreateCitiesApiClient(CreateHttpClient(handler));

        IReadOnlyList<ResidentialBuildingView> result = await client.GetResidentialBuildingsAsync(cityId, districtId, CancellationToken.None);

        Assert.Equal(buildings, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith(
            $"/api/cities/{cityId}/residential-buildings?districtId={districtId}",
            request.RequestUri,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task CitiesApiClientGetProvisioningStatusAsync_WhenResponseIsSuccessful_ReturnsView()
    {
        Guid cityId = Guid.Parse("eb1ff07a-1459-4d22-a54f-e7c6f69b1007");
        CityProvisioningStatusView provisioning = CreateCityProvisioningStatusView(cityId);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, provisioning))
        };
        ICitiesApiClient client = CreateCitiesApiClient(CreateHttpClient(handler));

        CityProvisioningStatusView result = await client.GetProvisioningStatusAsync(cityId, CancellationToken.None);

        Assert.Equal(provisioning, result);
    }

    [Fact]
    public async Task CitiesApiClientDeleteCityAsync_WhenDownstreamFails_ThrowsDownstreamServiceException()
    {
        Guid cityId = Guid.Parse("7e104fdf-11ff-44ff-96a7-aa68d47e39f2");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.InternalServerError, "{\"error\":\"delete-failed\"}"))
        };
        ICitiesApiClient client = CreateCitiesApiClient(CreateHttpClient(handler));

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => client.DeleteCityAsync(cityId, CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("delete-failed", exception.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TripsApiClientGetActiveTripsAsync_WhenResponseIsSuccessful_ReturnsTrips()
    {
        Guid cityId = Guid.Parse("12ca89b9-9084-4e7f-a5f0-0e25af62afc6");
        CityActiveTripView[] trips =
        [
            new CityActiveTripView(
                TripId: Guid.Parse("00e11d77-f9be-40de-b7aa-c4db94fe7f84"),
                CityId: cityId,
                TravellerEntityId: Guid.Parse("7d626fbe-5670-4c43-8fcf-1a5a415edbb9"),
                Subject: "Resident",
                Purpose: "Commute",
                Profile: "Worker",
                Status: "Active",
                MovementCapabilityIndex: 0.92m,
                UsedDynamicRoadConditions: true,
                PlannedAtTickId: 12,
                ConditionsEffectiveTickId: 12,
                LastAdvancedTickId: 13,
                StartedAtSimTimeUtc: new DateTimeOffset(2048, 6, 3, 7, 30, 0, TimeSpan.Zero),
                LastAdvancedAtSimTimeUtc: new DateTimeOffset(2048, 6, 3, 7, 35, 0, TimeSpan.Zero),
                ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(2048, 6, 3, 7, 50, 0, TimeSpan.Zero),
                ArrivedAtSimTimeUtc: null,
                CurrentProgressIndex: 0.4m,
                TotalDistanceMeters: 6000m,
                DistanceTravelledMeters: 2400m,
                RemainingDistanceMeters: 3600m,
                PlannedTravelTimeMinutes: 20m,
                AdjustedTravelTimeMinutes: 25m,
                From: new CityActiveTripEndpointView(
                    Kind: "ResidentialBuilding",
                    EntityId: Guid.Parse("2c01876a-e976-4f01-a427-c6de7c1be51a"),
                    DistrictId: Guid.Parse("615c93f1-8f06-4992-8a4e-6fef181591d5"),
                    RoadNodeId: Guid.Parse("e087ed92-e552-4595-a7c4-a2f2416e2706"),
                    Name: "Home",
                    PositionX: 4.2m,
                    PositionY: 6.8m),
                To: new CityActiveTripEndpointView(
                    Kind: "Workplace",
                    EntityId: Guid.Parse("aa64f388-7f76-4cfa-a964-9d539e3b1a6d"),
                    DistrictId: Guid.Parse("7b6ecb38-4db0-4749-86e4-e3ee73b095b4"),
                    RoadNodeId: Guid.Parse("41f9499e-e9e0-44c1-836a-85e4323b3ae3"),
                    Name: "Factory",
                    PositionX: 12.9m,
                    PositionY: 10.1m),
                Current: new CityActiveTripProgressView(
                    DistrictId: Guid.Parse("7b6ecb38-4db0-4749-86e4-e3ee73b095b4"),
                    RoadSegmentId: Guid.Parse("db99c51f-57cd-47d4-b48a-774fa2698404"),
                    SegmentProgressIndex: 0.58m,
                    PositionX: 6.7m,
                    PositionY: 8.4m))
        ];
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse<IReadOnlyList<CityActiveTripView>>(HttpStatusCode.OK, trips))
        };
        ITripsApiClient client = CreateTripsApiClient(CreateHttpClient(handler));

        IReadOnlyList<CityActiveTripView> result = await client.GetActiveTripsAsync(cityId, CancellationToken.None);

        Assert.Equal(trips, result);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/cities/{cityId}/trips/active", request.RequestUri, StringComparison.Ordinal);
    }
}
