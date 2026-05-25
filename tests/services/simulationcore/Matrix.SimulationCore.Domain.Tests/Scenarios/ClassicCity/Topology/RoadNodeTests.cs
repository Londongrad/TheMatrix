using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class RoadNodeTests
    {
        [Fact]
        public void Create_WithValidValues_TrimsName_AndNormalizesCoordinates()
        {
            RoadNode roadNode = TopologyTestData.CreateRoadNode();

            Assert.Equal(
                expected: TopologyTestData.CityId,
                actual: roadNode.CityId);
            Assert.Equal(
                expected: TopologyTestData.DistrictId,
                actual: roadNode.DistrictId);
            Assert.Equal(
                expected: "North Junction",
                actual: roadNode.Name);
            Assert.Equal(
                expected: RoadNodeType.Junction,
                actual: roadNode.Type);
            Assert.Equal(
                expected: 18.765m,
                actual: roadNode.PositionX);
            Assert.Equal(
                expected: 72.112m,
                actual: roadNode.PositionY);
            Assert.Equal(
                expected: TopologyTestData.CreatedAtUtc,
                actual: roadNode.CreatedAtUtc);
            Assert.Empty(roadNode.DomainEvents);
        }

        [Fact]
        public void Create_WithInvalidType_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => RoadNode.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                name: "Node",
                type: (RoadNodeType)999,
                positionX: 20m,
                positionY: 30m,
                createdAtUtc: TopologyTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Domain.Guard.InvalidEnum",
                actual: exception.Code);
            Assert.Equal(
                expected: "type",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithTooLongName_ThrowsDomainException()
        {
            string tooLong = new(
                c: 'n',
                count: RoadNode.MaxNameLength + 1);

            DomainException exception = Assert.Throws<DomainException>(() => RoadNode.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                name: tooLong,
                type: RoadNodeType.Junction,
                positionX: 20m,
                positionY: 30m,
                createdAtUtc: TopologyTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "SimulationCore.Topology.RoadNode.Name.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Name",
                actual: exception.PropertyName);
        }
    }
}
