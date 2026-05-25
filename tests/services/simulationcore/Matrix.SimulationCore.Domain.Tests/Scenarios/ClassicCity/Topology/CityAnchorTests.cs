using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class CityAnchorTests
    {
        [Fact]
        public void Create_WithValidValues_SetsProperties_AndNormalizesCoordinates()
        {
            CityAnchor cityAnchor = TopologyTestData.CreateCityAnchor();

            Assert.Equal(
                expected: TopologyTestData.CityId,
                actual: cityAnchor.CityId);
            Assert.Equal(
                expected: TopologyTestData.DistrictId,
                actual: cityAnchor.DistrictId);
            Assert.Equal(
                expected: TopologyTestData.RoadNodeId,
                actual: cityAnchor.AccessRoadNodeId);
            Assert.Equal(
                expected: new CityAnchorName("Central Hospital"),
                actual: cityAnchor.Name);
            Assert.Equal(
                expected: CityAnchorType.Hospital,
                actual: cityAnchor.Type);
            Assert.Equal(
                expected: 1200,
                actual: cityAnchor.Capacity);
            Assert.Equal(
                expected: 22.346m,
                actual: cityAnchor.PositionX);
            Assert.Equal(
                expected: 55.432m,
                actual: cityAnchor.PositionY);
            Assert.Equal(
                expected: TopologyTestData.CreatedAtUtc,
                actual: cityAnchor.CreatedAtUtc);
            Assert.Empty(cityAnchor.DomainEvents);
        }

        [Fact]
        public void Create_WithInvalidType_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityAnchor.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                accessRoadNodeId: TopologyTestData.RoadNodeId,
                name: new CityAnchorName("Central Hospital"),
                type: (CityAnchorType)999,
                capacity: 1200,
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
        public void Create_WithCapacityOutsideAllowedRange_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityAnchor.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                accessRoadNodeId: TopologyTestData.RoadNodeId,
                name: new CityAnchorName("Central Hospital"),
                type: CityAnchorType.Hospital,
                capacity: CityAnchor.MinCapacity - 1,
                positionX: 20m,
                positionY: 30m,
                createdAtUtc: TopologyTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "SimulationCore.Topology.CityAnchor.Capacity.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "Capacity",
                actual: exception.PropertyName);
        }
    }
}
