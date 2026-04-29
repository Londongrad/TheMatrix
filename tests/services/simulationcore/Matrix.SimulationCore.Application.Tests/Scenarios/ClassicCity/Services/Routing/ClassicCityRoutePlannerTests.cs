using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Routing;

public sealed class ClassicCityRoutePlannerTests
{
    private readonly ClassicCityRoutePlanner _planner = new();

    [Fact]
    public void Plan_WhenPointsShareRoadNode_ReturnsAccessibleZeroLengthRoute()
    {
        Guid cityId = Guid.NewGuid();
        var districtId = new DistrictId(Guid.NewGuid());
        RoadNode sharedNode = CreateRoadNode(cityId, districtId, "Shared Junction", 10m, 20m);
        CityRoutePointDto from = CreatePoint(sharedNode, districtId, "Origin");
        CityRoutePointDto to = CreatePoint(sharedNode, districtId, "Destination");
        var conditions = new CityRoadSegmentConditionsSnapshot(
            CityId: cityId,
            EffectiveTickId: 44,
            LastEvaluatedAtUtc: DateTimeOffset.Parse("2048-12-10T09:00:00+00:00"),
            RoadSupportIndex: 0.88m,
            Segments: []);

        CityRouteDto route = _planner.Plan(
            cityId: cityId,
            profile: CityRouteProfiles.Pedestrian,
            from: from,
            to: to,
            roadNodes: [sharedNode],
            roadSegments: [],
            segmentConditions: conditions);

        Assert.True(route.Accessible);
        Assert.True(route.UsedDynamicRoadConditions);
        Assert.Equal(44L, route.EffectiveTickId);
        Assert.Equal(0m, route.TotalDistanceMeters);
        Assert.Equal(0m, route.EstimatedTravelTimeMinutes);
        Assert.Equal(1m, route.OverallPassabilityIndex);
        Assert.Null(route.UnreachableReason);
        Assert.Empty(route.Segments);
    }

    [Fact]
    public void Plan_WhenNoTraversableGraphExists_ReturnsUnreachableRoute()
    {
        Guid cityId = Guid.NewGuid();
        var districtId = new DistrictId(Guid.NewGuid());
        RoadNode fromNode = CreateRoadNode(cityId, districtId, "From", 10m, 20m);
        RoadNode toNode = CreateRoadNode(cityId, districtId, "To", 40m, 50m);
        CityRoutePointDto from = CreatePoint(fromNode, districtId, "Origin");
        CityRoutePointDto to = CreatePoint(toNode, districtId, "Destination");

        CityRouteDto route = _planner.Plan(
            cityId: cityId,
            profile: CityRouteProfiles.ServiceVehicle,
            from: from,
            to: to,
            roadNodes: [fromNode],
            roadSegments: [],
            segmentConditions: null);

        Assert.False(route.Accessible);
        Assert.False(route.UsedDynamicRoadConditions);
        Assert.Equal(0m, route.TotalDistanceMeters);
        Assert.Equal(0m, route.EstimatedTravelTimeMinutes);
        Assert.Equal(0m, route.OverallPassabilityIndex);
        Assert.NotNull(route.UnreachableReason);
        Assert.Empty(route.Segments);
    }

    private static RoadNode CreateRoadNode(Guid cityId, DistrictId districtId, string name, decimal x, decimal y)
    {
        return RoadNode.Create(
            cityId: new CityId(cityId),
            districtId: districtId,
            name: name,
            type: RoadNodeType.Junction,
            positionX: x,
            positionY: y,
            createdAtUtc: TopologyTestSupport.CreatedAtUtc);
    }

    private static RoadSegment CreateRoadSegment(
        Guid cityId,
        DistrictId districtId,
        RoadNode from,
        RoadNode to,
        string name,
        RoadSegmentType type,
        decimal lengthMeters)
    {
        return RoadSegment.Create(
            cityId: new CityId(cityId),
            districtId: districtId,
            fromRoadNodeId: from.Id,
            toRoadNodeId: to.Id,
            name: name,
            type: type,
            lengthMeters: lengthMeters,
            createdAtUtc: TopologyTestSupport.CreatedAtUtc);
    }

    private static CityRoutePointDto CreatePoint(RoadNode node, DistrictId districtId, string name)
    {
        return new CityRoutePointDto(
            Kind: "RoadNode",
            EntityId: Guid.NewGuid(),
            DistrictId: districtId.Value,
            RoadNodeId: node.Id.Value,
            Name: name,
            PositionX: node.PositionX,
            PositionY: node.PositionY);
    }
}
