using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityMapTopology;

public sealed class GetCityMapTopologyQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedTopologySlices()
    {
        var cityId = new CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId, "Central");
        var roadNode = TopologyTestSupport.CreateRoadNode(cityId, district.Id, "Center Hub");
        var secondRoadNode = TopologyTestSupport.CreateRoadNode(cityId, district.Id, "East Hub");
        var roadSegment = TopologyTestSupport.CreateRoadSegment(cityId, district.Id, roadNode.Id, secondRoadNode.Id, "Center East");
        var building = TopologyTestSupport.CreateResidentialBuilding(cityId, district.Id, "Sunrise Tower");
        var anchor = TopologyTestSupport.CreateCityAnchor(cityId, district.Id, "City School");

        var districtRepository = new TopologyTestSupport.FakeDistrictRepository { Districts = [district] };
        var residentialBuildingRepository = new TopologyTestSupport.FakeResidentialBuildingRepository { Buildings = [building] };
        var cityAnchorRepository = new TopologyTestSupport.FakeCityAnchorRepository { Anchors = [anchor] };
        var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository { RoadNodes = [roadNode, secondRoadNode] };
        var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository { RoadSegments = [roadSegment] };
        var handler = new GetCityMapTopologyQueryHandler(
            districtRepository,
            residentialBuildingRepository,
            cityAnchorRepository,
            roadNodeRepository,
            roadSegmentRepository);

        var result = await handler.Handle(new GetCityMapTopologyQuery(cityId.Value), CancellationToken.None);

        Assert.Equal(cityId.Value, districtRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, residentialBuildingRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, cityAnchorRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, roadNodeRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, roadSegmentRepository.RequestedCityId!.Value.Value);

        Assert.Equal(cityId.Value, result.CityId);
        Assert.Single(result.Districts);
        Assert.Single(result.ResidentialBuildings);
        Assert.Single(result.Anchors);
        Assert.Equal(2, result.RoadNodes.Count);
        Assert.Single(result.RoadSegments);

        Assert.Equal("Central", result.Districts[0].Name);
        Assert.Equal("Sunrise Tower", result.ResidentialBuildings[0].Name);
        Assert.Equal("City School", result.Anchors[0].Name);
        Assert.Equal("Center Hub", result.RoadNodes[0].Name);
        Assert.Equal("Center East", result.RoadSegments[0].Name);
    }
}
