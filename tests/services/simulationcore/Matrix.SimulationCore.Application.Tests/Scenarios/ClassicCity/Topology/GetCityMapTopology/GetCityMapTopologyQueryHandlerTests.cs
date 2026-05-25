using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityMapTopology
{
    public sealed class GetCityMapTopologyQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedTopologySlices()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(
                cityId: cityId,
                name: "Central");
            RoadNode roadNode = TopologyTestSupport.CreateRoadNode(
                cityId: cityId,
                districtId: district.Id,
                name: "Center Hub");
            RoadNode secondRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: cityId,
                districtId: district.Id,
                name: "East Hub");
            RoadSegment roadSegment = TopologyTestSupport.CreateRoadSegment(
                cityId: cityId,
                districtId: district.Id,
                fromRoadNodeId: roadNode.Id,
                toRoadNodeId: secondRoadNode.Id,
                name: "Center East");
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: cityId,
                districtId: district.Id,
                name: "Sunrise Tower");
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: cityId,
                districtId: district.Id,
                name: "City School");

            var districtRepository = new TopologyTestSupport.FakeDistrictRepository
            {
                Districts = [district]
            };
            var residentialBuildingRepository = new TopologyTestSupport.FakeResidentialBuildingRepository
            {
                Buildings = [building]
            };
            var cityAnchorRepository = new TopologyTestSupport.FakeCityAnchorRepository
            {
                Anchors = [anchor]
            };
            var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository
            {
                RoadNodes =
                [
                    roadNode,
                    secondRoadNode
                ]
            };
            var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository
            {
                RoadSegments = [roadSegment]
            };
            var handler = new GetCityMapTopologyQueryHandler(
                districtRepository: districtRepository,
                residentialBuildingRepository: residentialBuildingRepository,
                cityAnchorRepository: cityAnchorRepository,
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository);

            CityMapTopologyDto result = await handler.Handle(
                request: new GetCityMapTopologyQuery(cityId.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId.Value,
                actual: districtRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: residentialBuildingRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: cityAnchorRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: roadNodeRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: roadSegmentRepository.RequestedCityId!.Value.Value);

            Assert.Equal(
                expected: cityId.Value,
                actual: result.CityId);
            Assert.Single(result.Districts);
            Assert.Single(result.ResidentialBuildings);
            Assert.Single(result.Anchors);
            Assert.Equal(
                expected: 2,
                actual: result.RoadNodes.Count);
            Assert.Single(result.RoadSegments);

            Assert.Equal(
                expected: "Central",
                actual: result.Districts[0].Name);
            Assert.Equal(
                expected: "Sunrise Tower",
                actual: result.ResidentialBuildings[0].Name);
            Assert.Equal(
                expected: "City School",
                actual: result.Anchors[0].Name);
            Assert.Equal(
                expected: "Center Hub",
                actual: result.RoadNodes[0].Name);
            Assert.Equal(
                expected: "Center East",
                actual: result.RoadSegments[0].Name);
        }
    }
}
