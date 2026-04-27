using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityRoadGraph;

public sealed class GetCityRoadGraphQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedDistrictsAndRoadSegments()
    {
        var cityId = new CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId, "Industrial");
        var fromRoadNode = TopologyTestSupport.CreateRoadNode(cityId, district.Id, "North Junction");
        var toRoadNode = TopologyTestSupport.CreateRoadNode(cityId, district.Id, "South Junction");
        var roadSegment = TopologyTestSupport.CreateRoadSegment(cityId, district.Id, fromRoadNode.Id, toRoadNode.Id, "Industrial Link");
        var districtRepository = new TopologyTestSupport.FakeDistrictRepository
        {
            Districts = [district]
        };
        var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository
        {
            RoadSegments = [roadSegment]
        };
        var handler = new GetCityRoadGraphQueryHandler(districtRepository, roadSegmentRepository);

        var result = await handler.Handle(new GetCityRoadGraphQuery(cityId.Value), CancellationToken.None);

        Assert.Equal(cityId.Value, districtRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, roadSegmentRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, result.CityId);

        var districtDto = Assert.Single(result.Districts);
        Assert.Equal(district.Id.Value, districtDto.DistrictId);
        Assert.Equal("Industrial", districtDto.Name);

        var roadSegmentDto = Assert.Single(result.RoadSegments);
        Assert.Equal(roadSegment.Id.Value, roadSegmentDto.RoadSegmentId);
        Assert.Equal(roadSegment.CityId.Value, roadSegmentDto.CityId);
        Assert.Equal(roadSegment.DistrictId.Value, roadSegmentDto.DistrictId);
        Assert.Equal(roadSegment.FromRoadNodeId.Value, roadSegmentDto.FromRoadNodeId);
        Assert.Equal(roadSegment.ToRoadNodeId.Value, roadSegmentDto.ToRoadNodeId);
        Assert.Equal("Industrial Link", roadSegmentDto.Name);
        Assert.Equal(roadSegment.Type.ToString(), roadSegmentDto.Type);
    }
}
