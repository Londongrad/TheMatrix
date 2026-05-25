using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class ResidentialBuildingTests
    {
        [Fact]
        public void Create_WithValidValues_SetsProperties_AndNormalizesCoordinates()
        {
            ResidentialBuilding building = TopologyTestData.CreateResidentialBuilding();

            Assert.Equal(
                expected: TopologyTestData.CityId,
                actual: building.CityId);
            Assert.Equal(
                expected: TopologyTestData.DistrictId,
                actual: building.DistrictId);
            Assert.Equal(
                expected: TopologyTestData.RoadNodeId,
                actual: building.AccessRoadNodeId);
            Assert.Equal(
                expected: new ResidentialBuildingName("Tower A"),
                actual: building.Name);
            Assert.Equal(
                expected: ResidentialBuildingType.Tower,
                actual: building.Type);
            Assert.Equal(
                expected: ResidentCapacity.From(380),
                actual: building.ResidentCapacity);
            Assert.Equal(
                expected: 40.126m,
                actual: building.PositionX);
            Assert.Equal(
                expected: 60.444m,
                actual: building.PositionY);
            Assert.Equal(
                expected: TopologyTestData.CreatedAtUtc,
                actual: building.CreatedAtUtc);
            Assert.Empty(building.DomainEvents);
        }

        [Fact]
        public void Rename_WhenNameChanges_UpdatesName()
        {
            ResidentialBuilding building = TopologyTestData.CreateResidentialBuilding();

            building.Rename(new ResidentialBuildingName("Tower B"));

            Assert.Equal(
                expected: new ResidentialBuildingName("Tower B"),
                actual: building.Name);
        }

        [Fact]
        public void ChangeResidentCapacity_WhenValueChanges_UpdatesCapacity()
        {
            ResidentialBuilding building = TopologyTestData.CreateResidentialBuilding();
            var newCapacity = ResidentCapacity.From(450);

            building.ChangeResidentCapacity(newCapacity);

            Assert.Equal(
                expected: newCapacity,
                actual: building.ResidentCapacity);
        }

        [Fact]
        public void Create_WithInvalidType_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => ResidentialBuilding.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                accessRoadNodeId: TopologyTestData.RoadNodeId,
                name: new ResidentialBuildingName("Tower A"),
                type: (ResidentialBuildingType)999,
                residentCapacity: ResidentCapacity.From(380),
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
    }
}
