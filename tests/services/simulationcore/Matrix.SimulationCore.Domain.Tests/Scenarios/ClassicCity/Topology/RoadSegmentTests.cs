using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Topology;

public sealed class RoadSegmentTests
{
    [Fact]
    public void Create_WithValidValues_TrimsName_AndNormalizesLength()
    {
        var roadSegment = TopologyTestData.CreateRoadSegment();

        Assert.Equal(TopologyTestData.CityId, roadSegment.CityId);
        Assert.Equal(TopologyTestData.DistrictId, roadSegment.DistrictId);
        Assert.Equal(TopologyTestData.RoadNodeId, roadSegment.FromRoadNodeId);
        Assert.Equal(TopologyTestData.AlternativeRoadNodeId, roadSegment.ToRoadNodeId);
        Assert.Equal("Main Artery", roadSegment.Name);
        Assert.Equal(RoadSegmentType.Arterial, roadSegment.Type);
        Assert.Equal(154.56m, roadSegment.LengthMeters);
        Assert.Equal(TopologyTestData.CreatedAtUtc, roadSegment.CreatedAtUtc);
        Assert.Empty(roadSegment.DomainEvents);
    }

    [Fact]
    public void Create_WhenEndpointsMatch_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => RoadSegment.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            fromRoadNodeId: TopologyTestData.RoadNodeId,
            toRoadNodeId: TopologyTestData.RoadNodeId,
            name: "Main Artery",
            type: RoadSegmentType.Arterial,
            lengthMeters: 150m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("SimulationCore.Topology.RoadSegment.Endpoints.Invalid", exception.Code);
        Assert.Equal("toRoadNodeId", exception.PropertyName);
    }

    [Fact]
    public void Create_WithInvalidType_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => RoadSegment.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            fromRoadNodeId: TopologyTestData.RoadNodeId,
            toRoadNodeId: TopologyTestData.AlternativeRoadNodeId,
            name: "Main Artery",
            type: (RoadSegmentType)999,
            lengthMeters: 150m,
            createdAtUtc: TopologyTestData.CreatedAtUtc));

        Assert.Equal("Domain.Guard.InvalidEnum", exception.Code);
        Assert.Equal("type", exception.PropertyName);
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => RoadSegment.Create(
            cityId: TopologyTestData.CityId,
            districtId: TopologyTestData.DistrictId,
            fromRoadNodeId: TopologyTestData.RoadNodeId,
            toRoadNodeId: TopologyTestData.AlternativeRoadNodeId,
            name: "Main Artery",
            type: RoadSegmentType.Arterial,
            lengthMeters: 150m,
            createdAtUtc: TopologyTestData.NonUtcCreatedAt));

        Assert.Equal("SimulationCore.Topology.Timestamp.NotUtc", exception.Code);
        Assert.Equal("value", exception.PropertyName);
    }
}
