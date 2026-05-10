using System.Net;
using System.Net.Http.Json;
using Matrix.Resources.Infrastructure.SimulationCore;
using Matrix.Resources.Infrastructure.Tests.Http;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.SimulationCore;

public sealed class CityResupplyTripDispatcherTests
{
    [Fact]
    public async Task TryDispatchDistrictResupplyAsync_ReturnsFalseWhenTopologyIsMissing()
    {
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var dispatcher = new CityResupplyTripDispatcher(HttpClientTestSupport.CreateHttpClient(handler));

        bool dispatched = await dispatcher.TryDispatchDistrictResupplyAsync(
            cityId: CityId,
            focusDistrictId: Guid.NewGuid(),
            focus: "Fuel",
            intensity: "Medium",
            cancellationToken: CancellationToken.None);

        Assert.False(dispatched);
    }

    [Fact]
    public async Task TryDispatchDistrictResupplyAsync_PostsTripForResolvedDistrictHub()
    {
        Guid districtId = Guid.Parse("70000000-0000-0000-0000-000000000002");
        Guid centralDistrictId = Guid.Parse("70000000-0000-0000-0000-000000000003");
        Guid centralNodeId = Guid.Parse("70000000-0000-0000-0000-000000000011");
        Guid districtNodeId = Guid.Parse("70000000-0000-0000-0000-000000000012");
        var topology = new CityMapTopologyView(
            CityId: CityId,
            Districts:
            [
                new DistrictView(districtId, CityId, "North", 10m, 20m, CreatedAtUtc)
            ],
            ResidentialBuildings: [],
            Anchors: [],
            RoadNodes:
            [
                new RoadNodeView(centralNodeId, CityId, centralDistrictId, "Central Hub", "DistrictHub", 0m, 0m, CreatedAtUtc),
                new RoadNodeView(districtNodeId, CityId, districtId, "North Hub", "DistrictHub", 10m, 20m, CreatedAtUtc)
            ],
            RoadSegments: []);
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get)
                return Task.FromResult(HttpClientTestSupport.CreateJsonResponse(topology));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        });
        var dispatcher = new CityResupplyTripDispatcher(HttpClientTestSupport.CreateHttpClient(handler));

        bool dispatched = await dispatcher.TryDispatchDistrictResupplyAsync(
            cityId: CityId,
            focusDistrictId: districtId,
            focus: "Fuel",
            intensity: "High",
            cancellationToken: CancellationToken.None);

        Assert.True(dispatched);
        Assert.Equal(2, handler.Requests.Count);
        HttpRequestMessage tripRequestMessage = handler.Requests[1];
        Assert.Equal(HttpMethod.Post, tripRequestMessage.Method);
        Assert.Equal($"/api/cities/{CityId}/trips", tripRequestMessage.RequestUri!.PathAndQuery);

        DispatchCityTripRequest? tripRequest = await tripRequestMessage.Content!.ReadFromJsonAsync<DispatchCityTripRequest>();
        Assert.NotNull(tripRequest);
        Assert.Equal(centralNodeId, tripRequest!.From.Id);
        Assert.Equal(districtNodeId, tripRequest.To.Id);
        Assert.Equal(1.12m, tripRequest.MovementCapabilityIndex);
        Assert.Contains("Fuel", tripRequest.Subject);
    }

    [Fact]
    public async Task TryDispatchDistrictResupplyAsync_ReturnsFalseWhenDispatchFails()
    {
        Guid districtId = Guid.Parse("70000000-0000-0000-0000-000000000002");
        Guid centralDistrictId = Guid.Parse("70000000-0000-0000-0000-000000000003");
        Guid centralNodeId = Guid.Parse("70000000-0000-0000-0000-000000000011");
        Guid districtNodeId = Guid.Parse("70000000-0000-0000-0000-000000000012");
        var topology = new CityMapTopologyView(
            CityId: CityId,
            Districts:
            [
                new DistrictView(districtId, CityId, "North", 10m, 20m, CreatedAtUtc)
            ],
            ResidentialBuildings: [],
            Anchors: [],
            RoadNodes:
            [
                new RoadNodeView(centralNodeId, CityId, centralDistrictId, "Central Hub", "DistrictHub", 0m, 0m, CreatedAtUtc),
                new RoadNodeView(districtNodeId, CityId, districtId, "North Hub", "DistrictHub", 10m, 20m, CreatedAtUtc)
            ],
            RoadSegments: []);
        var handler = new FakeHttpMessageHandler((request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Get)
                return Task.FromResult(HttpClientTestSupport.CreateJsonResponse(topology));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
        });
        var dispatcher = new CityResupplyTripDispatcher(HttpClientTestSupport.CreateHttpClient(handler));

        bool dispatched = await dispatcher.TryDispatchDistrictResupplyAsync(
            cityId: CityId,
            focusDistrictId: districtId,
            focus: "Fuel",
            intensity: "Low",
            cancellationToken: CancellationToken.None);

        Assert.False(dispatched);
    }
}
