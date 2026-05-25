using System.Net;
using System.Net.Http.Json;
using Matrix.Resources.Infrastructure.SimulationCore;
using Matrix.Resources.Infrastructure.Tests.Http;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Xunit;
using static Matrix.Resources.Infrastructure.Tests.TestSupport.ResourcesInfrastructureTestSupport;

namespace Matrix.Resources.Infrastructure.Tests.SimulationCore
{
    public sealed class CityResupplyTripDispatcherTests
    {
        [Fact]
        public async Task TryDispatchDistrictResupplyAsync_ReturnsFalseWhenTopologyIsMissing()
        {
            var handler = new FakeHttpMessageHandler((
                    request,
                    cancellationToken) =>
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
            var districtId = Guid.Parse("70000000-0000-0000-0000-000000000002");
            var centralDistrictId = Guid.Parse("70000000-0000-0000-0000-000000000003");
            var centralNodeId = Guid.Parse("70000000-0000-0000-0000-000000000011");
            var districtNodeId = Guid.Parse("70000000-0000-0000-0000-000000000012");
            var topology = new CityMapTopologyView(
                CityId: CityId,
                Districts:
                [
                    new DistrictView(
                        DistrictId: districtId,
                        CityId: CityId,
                        Name: "North",
                        AnchorX: 10m,
                        AnchorY: 20m,
                        CreatedAtUtc: CreatedAtUtc)
                ],
                ResidentialBuildings: [],
                Anchors: [],
                RoadNodes:
                [
                    new RoadNodeView(
                        RoadNodeId: centralNodeId,
                        CityId: CityId,
                        DistrictId: centralDistrictId,
                        Name: "Central Hub",
                        Type: "DistrictHub",
                        PositionX: 0m,
                        PositionY: 0m,
                        CreatedAtUtc: CreatedAtUtc),
                    new RoadNodeView(
                        RoadNodeId: districtNodeId,
                        CityId: CityId,
                        DistrictId: districtId,
                        Name: "North Hub",
                        Type: "DistrictHub",
                        PositionX: 10m,
                        PositionY: 20m,
                        CreatedAtUtc: CreatedAtUtc)
                ],
                RoadSegments: []);
            var handler = new FakeHttpMessageHandler((
                request,
                cancellationToken) =>
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
            Assert.Equal(
                expected: 2,
                actual: handler.Requests.Count);
            HttpRequestMessage tripRequestMessage = handler.Requests[1];
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: tripRequestMessage.Method);
            Assert.Equal(
                expected: $"/api/cities/{CityId}/trips",
                actual: tripRequestMessage.RequestUri!.PathAndQuery);

            DispatchCityTripRequest? tripRequest =
                await tripRequestMessage.Content!.ReadFromJsonAsync<DispatchCityTripRequest>();
            Assert.NotNull(tripRequest);
            Assert.Equal(
                expected: centralNodeId,
                actual: tripRequest!.From.Id);
            Assert.Equal(
                expected: districtNodeId,
                actual: tripRequest.To.Id);
            Assert.Equal(
                expected: 1.12m,
                actual: tripRequest.MovementCapabilityIndex);
            Assert.Contains(
                expectedSubstring: "Fuel",
                actualString: tripRequest.Subject);
        }

        [Fact]
        public async Task TryDispatchDistrictResupplyAsync_ReturnsFalseWhenDispatchFails()
        {
            var districtId = Guid.Parse("70000000-0000-0000-0000-000000000002");
            var centralDistrictId = Guid.Parse("70000000-0000-0000-0000-000000000003");
            var centralNodeId = Guid.Parse("70000000-0000-0000-0000-000000000011");
            var districtNodeId = Guid.Parse("70000000-0000-0000-0000-000000000012");
            var topology = new CityMapTopologyView(
                CityId: CityId,
                Districts:
                [
                    new DistrictView(
                        DistrictId: districtId,
                        CityId: CityId,
                        Name: "North",
                        AnchorX: 10m,
                        AnchorY: 20m,
                        CreatedAtUtc: CreatedAtUtc)
                ],
                ResidentialBuildings: [],
                Anchors: [],
                RoadNodes:
                [
                    new RoadNodeView(
                        RoadNodeId: centralNodeId,
                        CityId: CityId,
                        DistrictId: centralDistrictId,
                        Name: "Central Hub",
                        Type: "DistrictHub",
                        PositionX: 0m,
                        PositionY: 0m,
                        CreatedAtUtc: CreatedAtUtc),
                    new RoadNodeView(
                        RoadNodeId: districtNodeId,
                        CityId: CityId,
                        DistrictId: districtId,
                        Name: "North Hub",
                        Type: "DistrictHub",
                        PositionX: 10m,
                        PositionY: 20m,
                        CreatedAtUtc: CreatedAtUtc)
                ],
                RoadSegments: []);
            var handler = new FakeHttpMessageHandler((
                request,
                cancellationToken) =>
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
}
