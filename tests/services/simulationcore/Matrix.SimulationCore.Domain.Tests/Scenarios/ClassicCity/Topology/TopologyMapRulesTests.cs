using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class TopologyMapRulesTests
    {
        [Fact]
        public void NormalizeCoordinate_RoundsToThreeDecimals_AwayFromZero()
        {
            decimal normalized = TopologyMapRules.NormalizeCoordinate(
                value: 12.3456m,
                propertyName: "AnchorX");

            Assert.Equal(
                expected: 12.346m,
                actual: normalized);
        }

        [Fact]
        public void NormalizeCoordinate_WhenOutsideAllowedRange_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => TopologyMapRules.NormalizeCoordinate(
                value: 100.001m,
                propertyName: "AnchorY"));

            Assert.Equal(
                expected: "SimulationCore.Topology.Coordinate.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "AnchorY",
                actual: exception.PropertyName);
        }

        [Fact]
        public void NormalizeRoadSegmentLength_RoundsToTwoDecimals_AwayFromZero()
        {
            decimal normalized = TopologyMapRules.NormalizeRoadSegmentLength(
                value: 154.555m,
                propertyName: "LengthMeters");

            Assert.Equal(
                expected: 154.56m,
                actual: normalized);
        }

        [Fact]
        public void NormalizeRoadSegmentLength_WhenOutsideAllowedRange_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(()
                => TopologyMapRules.NormalizeRoadSegmentLength(
                    value: 9.994m,
                    propertyName: "LengthMeters"));

            Assert.Equal(
                expected: "SimulationCore.Topology.RoadSegment.Length.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "LengthMeters",
                actual: exception.PropertyName);
        }
    }
}
