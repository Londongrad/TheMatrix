using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class TopologyMapRulesTests
{
    [Fact]
    public void NormalizeCoordinate_RoundsToThreeDecimals_AwayFromZero()
    {
        var normalized = TopologyMapRules.NormalizeCoordinate(
            value: 12.3456m,
            propertyName: "AnchorX");

        Assert.Equal(12.346m, normalized);
    }

    [Fact]
    public void NormalizeCoordinate_WhenOutsideAllowedRange_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => TopologyMapRules.NormalizeCoordinate(
            value: 100.001m,
            propertyName: "AnchorY"));

        Assert.Equal("SimulationCore.Topology.Coordinate.OutOfRange", exception.Code);
        Assert.Equal("AnchorY", exception.PropertyName);
    }

    [Fact]
    public void NormalizeRoadSegmentLength_RoundsToTwoDecimals_AwayFromZero()
    {
        var normalized = TopologyMapRules.NormalizeRoadSegmentLength(
            value: 154.555m,
            propertyName: "LengthMeters");

        Assert.Equal(154.56m, normalized);
    }

    [Fact]
    public void NormalizeRoadSegmentLength_WhenOutsideAllowedRange_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => TopologyMapRules.NormalizeRoadSegmentLength(
            value: 9.994m,
            propertyName: "LengthMeters"));

        Assert.Equal("SimulationCore.Topology.RoadSegment.Length.OutOfRange", exception.Code);
        Assert.Equal("LengthMeters", exception.PropertyName);
    }
}
