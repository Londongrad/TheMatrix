using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class CityAnchorTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties_AndNormalizesCoordinates()
    {
        var cityAnchor = TopologyTestData.CreateCityAnchor();

        Assert.Equal(TopologyTestData.CityId, cityAnchor.CityId);
        Assert.Equal(TopologyTestData.DistrictId, cityAnchor.DistrictId);
        Assert.Equal(TopologyTestData.RoadNodeId, cityAnchor.AccessRoadNodeId);
        Assert.Equal(new CityAnchorName("Central Hospital"), cityAnchor.Name);
        Assert.Equal(CityAnchorType.Hospital, cityAnchor.Type);
        Assert.Equal(1200, cityAnchor.Capacity);
        Assert.Equal(22.346m, cityAnchor.PositionX);
        Assert.Equal(55.432m, cityAnchor.PositionY);
        Assert.Equal(TopologyTestData.CreatedAtUtc, cityAnchor.CreatedAtUtc);
        Assert.Empty(cityAnchor.DomainEvents);
    }

    [Fact]
    public void Create_WithInvalidType_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityAnchor.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            accessRoadNodeId: TopologyTestData.RoadNodeId,
            name: new CityAnchorName("Central Hospital"),
            type: (CityAnchorType)999,
            capacity: 1200,
            positionX: 20m,
            positionY: 30m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("type", exception.PropertyName);
    }

    [Fact]
    public void Create_WithCapacityOutsideAllowedRange_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityAnchor.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            accessRoadNodeId: TopologyTestData.RoadNodeId,
            name: new CityAnchorName("Central Hospital"),
            type: CityAnchorType.Hospital,
            capacity: CityAnchor.MinCapacity - 1,
            positionX: 20m,
            positionY: 30m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("SimulationCore.Topology.CityAnchor.Capacity.OutOfRange", exception.Code);
        Assert.Equal("Capacity", exception.PropertyName);
    }
}
