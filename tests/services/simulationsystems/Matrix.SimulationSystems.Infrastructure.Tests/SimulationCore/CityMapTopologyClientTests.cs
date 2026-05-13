using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Topology.Views;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Infrastructure.SimulationCore;
using Matrix.SimulationSystems.Infrastructure.Tests.Http;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.SimulationCore;

public sealed class CityMapTopologyClientTests
{
    [Fact]
    public async Task GetRoadGraphAsync_WhenResponseIsSuccessful_MapsPayload()
    {
        Guid districtId = Guid.Parse("2c7c0480-1db6-4e0d-a2b2-57046ec68167");
        Guid roadSegmentId = Guid.Parse("8d51e6ef-cc91-42f0-8db0-b18b69f6a26d");
        CityRoadGraphView payload = new(
            CityId: CityId,
            Districts:
            [
                new DistrictView(
                    DistrictId: districtId,
                    CityId: CityId,
                    Name: "Harbor",
                    AnchorX: 14m,
                    AnchorY: 22m,
                    CreatedAtUtc: CreatedAtUtc)
            ],
            RoadSegments:
            [
                new RoadSegmentView(
                    RoadSegmentId: roadSegmentId,
                    CityId: CityId,
                    DistrictId: districtId,
                    FromRoadNodeId: Guid.Parse("9f782fd0-1749-4160-b436-fa82118c71e3"),
                    ToRoadNodeId: Guid.Parse("e6c537be-cfee-429e-8ed5-8dd50545525f"),
                    Name: "Pier Road",
                    Type: "Arterial",
                    LengthMeters: 420m,
                    CreatedAtUtc: CreatedAtUtc)
            ]);
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"/api/cities/{CityId}/road-graph", request.RequestUri!.PathAndQuery);
            return Task.FromResult(HttpClientTestSupport.CreateJsonResponse(payload));
        });
        var client = new CityMapTopologyClient(HttpClientTestSupport.CreateHttpClient(handler));

        var result = await client.GetRoadGraphAsync(CityId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CityId, result!.CityId);
        CityDistrictTopologyDto district = Assert.Single(result.Districts);
        Assert.Equal(districtId, district.DistrictId);
        CityRoadSegmentTopologyDto segment = Assert.Single(result.RoadSegments);
        Assert.Equal(roadSegmentId, segment.RoadSegmentId);
        Assert.Equal("Pier Road", segment.Name);
    }

    [Fact]
    public async Task GetRoadGraphAsync_WhenPayloadIsNull_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler((request, _) =>
            Task.FromResult(HttpClientTestSupport.CreateStringResponse("null")));
        var client = new CityMapTopologyClient(HttpClientTestSupport.CreateHttpClient(handler));

        var result = await client.GetRoadGraphAsync(CityId, CancellationToken.None);

        Assert.Null(result);
    }
}
