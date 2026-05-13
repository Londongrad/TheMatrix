using System.Net;
using System.Text.Json;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationSystems.Infrastructure.SimulationCore;
using Matrix.SimulationSystems.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.SimulationCore;

public sealed class CityOperationalTripDispatcherTests
{
    [Fact]
    public async Task TryDispatchUtilityIncidentResponseAsync_WhenTopologyIsMissing_ReturnsFalse()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
            Task.FromResult(HttpClientTestSupport.CreateStringResponse("null")));
        var dispatcher = new CityOperationalTripDispatcher(HttpClientTestSupport.CreateHttpClient(handler));

        bool dispatched = await dispatcher.TryDispatchUtilityIncidentResponseAsync(
            cityId: CityId,
            focusDistrictId: Guid.Parse("dc5f51de-f411-4d2e-aa06-3f85d7ad5a17"),
            focus: "DistrictHub",
            intensity: "Heavy",
            cancellationToken: CancellationToken.None);

        Assert.False(dispatched);
        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"/api/cities/{CityId}/map", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task TryDispatchUtilityIncidentResponseAsync_WhenTopologyAndTripDispatchSucceed_PostsExpectedPayload()
    {
        Guid districtId = Guid.Parse("dc5f51de-f411-4d2e-aa06-3f85d7ad5a17");
        Guid centralHubId = Guid.Parse("aa523b27-bb9d-40fd-b528-d7fb890520e1");
        Guid districtHubId = Guid.Parse("b0a1d5ea-f487-455a-8c8e-f446c0aa6de9");
        string? postedJson = null;
        var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            if (request.Method == HttpMethod.Get)
                return HttpClientTestSupport.CreateJsonResponse(CreateTopology(districtId, centralHubId, districtHubId));

            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/api/cities/{CityId}/trips", request.RequestUri!.PathAndQuery);
            postedJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var dispatcher = new CityOperationalTripDispatcher(HttpClientTestSupport.CreateHttpClient(handler));

        bool dispatched = await dispatcher.TryDispatchUtilityIncidentResponseAsync(
            cityId: CityId,
            focusDistrictId: districtId,
            focus: "WaterDistribution",
            intensity: "Heavy",
            cancellationToken: CancellationToken.None);

        Assert.True(dispatched);
        Assert.NotNull(postedJson);
        DispatchCityTripRequest? payload = JsonSerializer.Deserialize<DispatchCityTripRequest>(
            postedJson!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal("RoadNode", payload!.From.Kind);
        Assert.Equal(centralHubId, payload.From.Id);
        Assert.Equal("RoadNode", payload.To.Kind);
        Assert.Equal(districtHubId, payload.To.Id);
        Assert.Equal("ServiceResponse", payload.Purpose);
        Assert.Equal("ServiceVehicle", payload.Profile);
        Assert.Equal(1.18m, payload.MovementCapabilityIndex);
        Assert.Equal("Harbor utility response (WaterDistribution)", payload.Subject);
    }

    [Fact]
    public async Task TryDispatchUtilityIncidentResponseAsync_WhenTopologyRequestThrows_ReturnsFalse()
    {
        var handler = new FakeHttpMessageHandler((request, _) => throw new HttpRequestException("boom"));
        var dispatcher = new CityOperationalTripDispatcher(HttpClientTestSupport.CreateHttpClient(handler));

        bool dispatched = await dispatcher.TryDispatchUtilityIncidentResponseAsync(
            cityId: CityId,
            focusDistrictId: Guid.Parse("dc5f51de-f411-4d2e-aa06-3f85d7ad5a17"),
            focus: "RoadAccess",
            intensity: "Standard",
            cancellationToken: CancellationToken.None);

        Assert.False(dispatched);
    }

    private static CityMapTopologyView CreateTopology(Guid districtId, Guid centralHubId, Guid districtHubId)
    {
        Guid centralDistrictId = Guid.Parse("66021b1e-8076-4329-97a2-fcb58d8ce35d");

        return new CityMapTopologyView(
            CityId: CityId,
            Districts:
            [
                new DistrictView(
                    DistrictId: centralDistrictId,
                    CityId: CityId,
                    Name: "Central",
                    AnchorX: 6m,
                    AnchorY: 7m,
                    CreatedAtUtc: CreatedAtUtc),
                new DistrictView(
                    DistrictId: districtId,
                    CityId: CityId,
                    Name: "Harbor",
                    AnchorX: 12m,
                    AnchorY: 18m,
                    CreatedAtUtc: CreatedAtUtc)
            ],
            ResidentialBuildings: [],
            Anchors: [],
            RoadNodes:
            [
                new RoadNodeView(
                    RoadNodeId: centralHubId,
                    CityId: CityId,
                    DistrictId: centralDistrictId,
                    Name: "Central Hub",
                    Type: "DistrictHub",
                    PositionX: 10m,
                    PositionY: 10m,
                    CreatedAtUtc: CreatedAtUtc),
                new RoadNodeView(
                    RoadNodeId: districtHubId,
                    CityId: CityId,
                    DistrictId: districtId,
                    Name: "Harbor Hub",
                    Type: "DistrictHub",
                    PositionX: 20m,
                    PositionY: 22m,
                    CreatedAtUtc: CreatedAtUtc)
            ],
            RoadSegments: []);
    }
}
