using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class RoadNodeTests
{
    [Fact]
    public void Create_WithValidValues_TrimsName_AndNormalizesCoordinates()
    {
        var roadNode = TopologyTestData.CreateRoadNode();

        Assert.Equal(TopologyTestData.CityId, roadNode.CityId);
        Assert.Equal(TopologyTestData.DistrictId, roadNode.DistrictId);
        Assert.Equal("North Junction", roadNode.Name);
        Assert.Equal(RoadNodeType.Junction, roadNode.Type);
        Assert.Equal(18.765m, roadNode.PositionX);
        Assert.Equal(72.112m, roadNode.PositionY);
        Assert.Equal(TopologyTestData.CreatedAtUtc, roadNode.CreatedAtUtc);
        Assert.Empty(roadNode.DomainEvents);
    }

    [Fact]
    public void Create_WithInvalidType_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => RoadNode.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            name: "Node",
            type: (RoadNodeType)999,
            positionX: 20m,
            positionY: 30m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("type", exception.PropertyName);
    }

    [Fact]
    public void Create_WithTooLongName_ThrowsDomainException()
    {
        var tooLong = new string('n', RoadNode.MaxNameLength + 1);

        var exception = Assert.Throws<DomainException>(() => RoadNode.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            name: tooLong,
            type: RoadNodeType.Junction,
            positionX: 20m,
            positionY: 30m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("SimulationCore.Topology.RoadNode.Name.TooLong", exception.Code);
        Assert.Equal("Name", exception.PropertyName);
    }
}
