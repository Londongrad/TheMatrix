using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.GetCityRoadGraph
{
    public sealed class GetCityRoadGraphQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedDistrictsAndRoadSegments()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(
                cityId: cityId,
                name: "Industrial");
            RoadNode fromRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: cityId,
                districtId: district.Id,
                name: "North Junction");
            RoadNode toRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: cityId,
                districtId: district.Id,
                name: "South Junction");
            RoadSegment roadSegment = TopologyTestSupport.CreateRoadSegment(
                cityId: cityId,
                districtId: district.Id,
                fromRoadNodeId: fromRoadNode.Id,
                toRoadNodeId: toRoadNode.Id,
                name: "Industrial Link");
            var districtRepository = new TopologyTestSupport.FakeDistrictRepository
            {
                Districts = [district]
            };
            var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository
            {
                RoadSegments = [roadSegment]
            };
            var handler = new GetCityRoadGraphQueryHandler(
                districtRepository: districtRepository,
                roadSegmentRepository: roadSegmentRepository);

            CityRoadGraphDto result = await handler.Handle(
                request: new GetCityRoadGraphQuery(cityId.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId.Value,
                actual: districtRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: roadSegmentRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: result.CityId);

            DistrictDto districtDto = Assert.Single(result.Districts);
            Assert.Equal(
                expected: district.Id.Value,
                actual: districtDto.DistrictId);
            Assert.Equal(
                expected: "Industrial",
                actual: districtDto.Name);

            RoadSegmentDto roadSegmentDto = Assert.Single(result.RoadSegments);
            Assert.Equal(
                expected: roadSegment.Id.Value,
                actual: roadSegmentDto.RoadSegmentId);
            Assert.Equal(
                expected: roadSegment.CityId.Value,
                actual: roadSegmentDto.CityId);
            Assert.Equal(
                expected: roadSegment.DistrictId.Value,
                actual: roadSegmentDto.DistrictId);
            Assert.Equal(
                expected: roadSegment.FromRoadNodeId.Value,
                actual: roadSegmentDto.FromRoadNodeId);
            Assert.Equal(
                expected: roadSegment.ToRoadNodeId.Value,
                actual: roadSegmentDto.ToRoadNodeId);
            Assert.Equal(
                expected: "Industrial Link",
                actual: roadSegmentDto.Name);
            Assert.Equal(
                expected: roadSegment.Type.ToString(),
                actual: roadSegmentDto.Type);
        }
    }
}
