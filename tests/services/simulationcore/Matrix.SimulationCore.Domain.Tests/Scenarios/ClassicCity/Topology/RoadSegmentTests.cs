using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology
{
    public sealed class RoadSegmentTests
    {
        [Fact]
        public void Create_WithValidValues_TrimsName_AndNormalizesLength()
        {
            RoadSegment roadSegment = TopologyTestData.CreateRoadSegment();

            Assert.Equal(
                expected: TopologyTestData.CityId,
                actual: roadSegment.CityId);
            Assert.Equal(
                expected: TopologyTestData.DistrictId,
                actual: roadSegment.DistrictId);
            Assert.Equal(
                expected: TopologyTestData.RoadNodeId,
                actual: roadSegment.FromRoadNodeId);
            Assert.Equal(
                expected: TopologyTestData.AlternativeRoadNodeId,
                actual: roadSegment.ToRoadNodeId);
            Assert.Equal(
                expected: "Main Artery",
                actual: roadSegment.Name);
            Assert.Equal(
                expected: RoadSegmentType.Arterial,
                actual: roadSegment.Type);
            Assert.Equal(
                expected: 154.56m,
                actual: roadSegment.LengthMeters);
            Assert.Equal(
                expected: TopologyTestData.CreatedAtUtc,
                actual: roadSegment.CreatedAtUtc);
            Assert.Empty(roadSegment.DomainEvents);
        }

        [Fact]
        public void Create_WhenEndpointsMatch_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => RoadSegment.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                fromRoadNodeId: TopologyTestData.RoadNodeId,
                toRoadNodeId: TopologyTestData.RoadNodeId,
                name: "Main Artery",
                type: RoadSegmentType.Arterial,
                lengthMeters: 150m,
                createdAtUtc: TopologyTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "SimulationCore.Topology.RoadSegment.Endpoints.Invalid",
                actual: exception.Code);
            Assert.Equal(
                expected: "toRoadNodeId",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithInvalidType_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => RoadSegment.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                fromRoadNodeId: TopologyTestData.RoadNodeId,
                toRoadNodeId: TopologyTestData.AlternativeRoadNodeId,
                name: "Main Artery",
                type: (RoadSegmentType)999,
                lengthMeters: 150m,
                createdAtUtc: TopologyTestData.CreatedAtUtc));

            Assert.Equal(
                expected: "Domain.Guard.InvalidEnum",
                actual: exception.Code);
            Assert.Equal(
                expected: "type",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithNonUtcTimestamp_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => RoadSegment.Create(
                cityId: TopologyTestData.CityId,
                districtId: TopologyTestData.DistrictId,
                fromRoadNodeId: TopologyTestData.RoadNodeId,
                toRoadNodeId: TopologyTestData.AlternativeRoadNodeId,
                name: "Main Artery",
                type: RoadSegmentType.Arterial,
                lengthMeters: 150m,
                createdAtUtc: TopologyTestData.NonUtcCreatedAt));

            Assert.Equal(
                expected: "SimulationCore.Topology.Timestamp.NotUtc",
                actual: exception.Code);
            Assert.Equal(
                expected: "value",
                actual: exception.PropertyName);
        }
    }
}
