using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class ResidentialBuildingTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties_AndNormalizesCoordinates()
    {
        var building = TopologyTestData.CreateResidentialBuilding();

        Assert.Equal(TopologyTestData.CityId, building.CityId);
        Assert.Equal(TopologyTestData.DistrictId, building.DistrictId);
        Assert.Equal(TopologyTestData.RoadNodeId, building.AccessRoadNodeId);
        Assert.Equal(new ResidentialBuildingName("Tower A"), building.Name);
        Assert.Equal(ResidentialBuildingType.Tower, building.Type);
        Assert.Equal(ResidentCapacity.From(380), building.ResidentCapacity);
        Assert.Equal(40.126m, building.PositionX);
        Assert.Equal(60.444m, building.PositionY);
        Assert.Equal(TopologyTestData.CreatedAtUtc, building.CreatedAtUtc);
        Assert.Empty(building.DomainEvents);
    }

    [Fact]
    public void Rename_WhenNameChanges_UpdatesName()
    {
        var building = TopologyTestData.CreateResidentialBuilding();

        building.Rename(new ResidentialBuildingName("Tower B"));

        Assert.Equal(new ResidentialBuildingName("Tower B"), building.Name);
    }

    [Fact]
    public void ChangeResidentCapacity_WhenValueChanges_UpdatesCapacity()
    {
        var building = TopologyTestData.CreateResidentialBuilding();
        var newCapacity = ResidentCapacity.From(450);

        building.ChangeResidentCapacity(newCapacity);

        Assert.Equal(newCapacity, building.ResidentCapacity);
    }

    [Fact]
    public void Create_WithInvalidType_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => ResidentialBuilding.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            accessRoadNodeId: TopologyTestData.RoadNodeId,
            name: new ResidentialBuildingName("Tower A"),
            type: (ResidentialBuildingType)999,
            residentCapacity: ResidentCapacity.From(380),
            positionX: 20m,
            positionY: 30m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("type", exception.PropertyName);
    }
}
