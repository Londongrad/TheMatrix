using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World;

public sealed class CityActiveTripSegmentTests
{
    [Fact]
    public void Create_WithValidValues_TrimsStrings_AndNormalizesCoordinates()
    {
        var segment = WorldTestData.CreateFirstSegment();

        Assert.Equal(0, segment.Sequence);
        Assert.Equal(WorldTestData.FirstRoadSegmentId, segment.RoadSegmentId);
        Assert.Equal(WorldTestData.FromDistrictId, segment.DistrictId);
        Assert.Equal(WorldTestData.FromRoadNodeId, segment.FromRoadNodeId);
        Assert.Equal(WorldTestData.MidRoadNodeId, segment.ToRoadNodeId);
        Assert.Equal("Segment A", segment.Name);
        Assert.Equal("arterial", segment.Type);
        Assert.Equal(120m, segment.LengthMeters);
        Assert.Equal(6m, segment.EstimatedTraversalMinutes);
        Assert.Equal(10.111m, segment.FromPositionX);
        Assert.Equal(20.222m, segment.FromPositionY);
        Assert.Equal(30.333m, segment.ToPositionX);
        Assert.Equal(40.444m, segment.ToPositionY);
    }

    [Fact]
    public void Create_WhenSequenceIsOutOfRange_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
            sequence: -1,
            roadSegmentId: WorldTestData.FirstRoadSegmentId,
            districtId: WorldTestData.FromDistrictId,
            fromRoadNodeId: WorldTestData.FromRoadNodeId,
            toRoadNodeId: WorldTestData.MidRoadNodeId,
            name: "Segment A",
            type: "arterial",
            lengthMeters: 120m,
            estimatedTraversalMinutes: 6m,
            fromPositionX: 10m,
            fromPositionY: 20m,
            toPositionX: 30m,
            toPositionY: 40m));

        Assert.Equal("SimulationCore.World.ActiveTripSegment.Sequence.OutOfRange", exception.Code);
        Assert.Equal("Sequence", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenNameIsTooLong_ThrowsDomainException()
    {
        var tooLong = new string('s', CityActiveTrip.MaxSubjectLength + 1);

        var exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
            sequence: 0,
            roadSegmentId: WorldTestData.FirstRoadSegmentId,
            districtId: WorldTestData.FromDistrictId,
            fromRoadNodeId: WorldTestData.FromRoadNodeId,
            toRoadNodeId: WorldTestData.MidRoadNodeId,
            name: tooLong,
            type: "arterial",
            lengthMeters: 120m,
            estimatedTraversalMinutes: 6m,
            fromPositionX: 10m,
            fromPositionY: 20m,
            toPositionX: 30m,
            toPositionY: 40m));

        Assert.Equal("SimulationCore.World.ActiveTripSegment.Name.TooLong", exception.Code);
        Assert.Equal("Name", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenTypeIsTooLong_ThrowsDomainException()
    {
        var tooLong = new string('t', 65);

        var exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
            sequence: 0,
            roadSegmentId: WorldTestData.FirstRoadSegmentId,
            districtId: WorldTestData.FromDistrictId,
            fromRoadNodeId: WorldTestData.FromRoadNodeId,
            toRoadNodeId: WorldTestData.MidRoadNodeId,
            name: "Segment A",
            type: tooLong,
            lengthMeters: 120m,
            estimatedTraversalMinutes: 6m,
            fromPositionX: 10m,
            fromPositionY: 20m,
            toPositionX: 30m,
            toPositionY: 40m));

        Assert.Equal("SimulationCore.World.ActiveTripSegment.Type.TooLong", exception.Code);
        Assert.Equal("Type", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenLengthIsOutOfRange_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
            sequence: 0,
            roadSegmentId: WorldTestData.FirstRoadSegmentId,
            districtId: WorldTestData.FromDistrictId,
            fromRoadNodeId: WorldTestData.FromRoadNodeId,
            toRoadNodeId: WorldTestData.MidRoadNodeId,
            name: "Segment A",
            type: "arterial",
            lengthMeters: 0m,
            estimatedTraversalMinutes: 6m,
            fromPositionX: 10m,
            fromPositionY: 20m,
            toPositionX: 30m,
            toPositionY: 40m));

        Assert.Equal("SimulationCore.World.ActiveTripSegment.Length.OutOfRange", exception.Code);
        Assert.Equal("LengthMeters", exception.PropertyName);
    }

    [Fact]
    public void Create_WhenEstimatedTraversalIsOutOfRange_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
            sequence: 0,
            roadSegmentId: WorldTestData.FirstRoadSegmentId,
            districtId: WorldTestData.FromDistrictId,
            fromRoadNodeId: WorldTestData.FromRoadNodeId,
            toRoadNodeId: WorldTestData.MidRoadNodeId,
            name: "Segment A",
            type: "arterial",
            lengthMeters: 120m,
            estimatedTraversalMinutes: 0m,
            fromPositionX: 10m,
            fromPositionY: 20m,
            toPositionX: 30m,
            toPositionY: 40m));

        Assert.Equal("SimulationCore.World.ActiveTripSegment.Traversal.OutOfRange", exception.Code);
        Assert.Equal("EstimatedTraversalMinutes", exception.PropertyName);
    }
}
