using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.World
{
    public sealed class CityActiveTripSegmentTests
    {
        [Fact]
        public void Create_WithValidValues_TrimsStrings_AndNormalizesCoordinates()
        {
            CityActiveTripSegment segment = WorldTestData.CreateFirstSegment();

            Assert.Equal(
                expected: 0,
                actual: segment.Sequence);
            Assert.Equal(
                expected: WorldTestData.FirstRoadSegmentId,
                actual: segment.RoadSegmentId);
            Assert.Equal(
                expected: WorldTestData.FromDistrictId,
                actual: segment.DistrictId);
            Assert.Equal(
                expected: WorldTestData.FromRoadNodeId,
                actual: segment.FromRoadNodeId);
            Assert.Equal(
                expected: WorldTestData.MidRoadNodeId,
                actual: segment.ToRoadNodeId);
            Assert.Equal(
                expected: "Segment A",
                actual: segment.Name);
            Assert.Equal(
                expected: "arterial",
                actual: segment.Type);
            Assert.Equal(
                expected: 120m,
                actual: segment.LengthMeters);
            Assert.Equal(
                expected: 6m,
                actual: segment.EstimatedTraversalMinutes);
            Assert.Equal(
                expected: 10.111m,
                actual: segment.FromPositionX);
            Assert.Equal(
                expected: 20.222m,
                actual: segment.FromPositionY);
            Assert.Equal(
                expected: 30.333m,
                actual: segment.ToPositionX);
            Assert.Equal(
                expected: 40.444m,
                actual: segment.ToPositionY);
        }

        [Fact]
        public void Create_WhenSequenceIsOutOfRange_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
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

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTripSegment.Sequence.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "Sequence",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenNameIsTooLong_ThrowsDomainException()
        {
            string tooLong = new(
                c: 's',
                count: CityActiveTrip.MaxSubjectLength + 1);

            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
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

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTripSegment.Name.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Name",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenTypeIsTooLong_ThrowsDomainException()
        {
            string tooLong = new(
                c: 't',
                count: 65);

            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
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

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTripSegment.Type.TooLong",
                actual: exception.Code);
            Assert.Equal(
                expected: "Type",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenLengthIsOutOfRange_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
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

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTripSegment.Length.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "LengthMeters",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WhenEstimatedTraversalIsOutOfRange_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityActiveTripSegment.Create(
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

            Assert.Equal(
                expected: "SimulationCore.World.ActiveTripSegment.Traversal.OutOfRange",
                actual: exception.Code);
            Assert.Equal(
                expected: "EstimatedTraversalMinutes",
                actual: exception.PropertyName);
        }
    }
}
