using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.Routing
{
    public sealed class ClassicCityRoutePlannerTests
    {
        private readonly ClassicCityRoutePlanner _planner = new();

        [Fact]
        public void Plan_WhenPointsShareRoadNode_ReturnsAccessibleZeroLengthRoute()
        {
            var cityId = Guid.NewGuid();
            var districtId = new DistrictId(Guid.NewGuid());
            RoadNode sharedNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "Shared Junction",
                x: 10m,
                y: 20m);
            CityRoutePointDto from = CreatePoint(
                node: sharedNode,
                districtId: districtId,
                name: "Origin");
            CityRoutePointDto to = CreatePoint(
                node: sharedNode,
                districtId: districtId,
                name: "Destination");
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
            Assert.Equal(
                expected: 44L,
                actual: route.EffectiveTickId);
            Assert.Equal(
                expected: 0m,
                actual: route.TotalDistanceMeters);
            Assert.Equal(
                expected: 0m,
                actual: route.EstimatedTravelTimeMinutes);
            Assert.Equal(
                expected: 1m,
                actual: route.OverallPassabilityIndex);
            Assert.Null(route.UnreachableReason);
            Assert.Empty(route.Segments);
        }

        [Fact]
        public void Plan_WhenNoTraversableGraphExists_ReturnsUnreachableRoute()
        {
            var cityId = Guid.NewGuid();
            var districtId = new DistrictId(Guid.NewGuid());
            RoadNode fromNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "From",
                x: 10m,
                y: 20m);
            RoadNode toNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "To",
                x: 40m,
                y: 50m);
            CityRoutePointDto from = CreatePoint(
                node: fromNode,
                districtId: districtId,
                name: "Origin");
            CityRoutePointDto to = CreatePoint(
                node: toNode,
                districtId: districtId,
                name: "Destination");

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
            Assert.Equal(
                expected: 0m,
                actual: route.TotalDistanceMeters);
            Assert.Equal(
                expected: 0m,
                actual: route.EstimatedTravelTimeMinutes);
            Assert.Equal(
                expected: 0m,
                actual: route.OverallPassabilityIndex);
            Assert.NotNull(route.UnreachableReason);
            Assert.Empty(route.Segments);
        }

        [Fact]
        public void Plan_WhenMultipleRoutesExist_ChoosesLowestTraversalCostPath()
        {
            var cityId = Guid.NewGuid();
            var districtId = new DistrictId(Guid.NewGuid());
            RoadNode fromNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "From",
                x: 10m,
                y: 20m);
            RoadNode middleNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "Middle",
                x: 20m,
                y: 20m);
            RoadNode toNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "To",
                x: 40m,
                y: 20m);
            RoadSegment firstHop = CreateRoadSegment(
                cityId: cityId,
                districtId: districtId,
                from: fromNode,
                to: middleNode,
                name: "First Hop",
                type: RoadSegmentType.Collector,
                lengthMeters: 78m);
            RoadSegment secondHop = CreateRoadSegment(
                cityId: cityId,
                districtId: districtId,
                from: middleNode,
                to: toNode,
                name: "Second Hop",
                type: RoadSegmentType.Collector,
                lengthMeters: 78m);
            RoadSegment direct = CreateRoadSegment(
                cityId: cityId,
                districtId: districtId,
                from: fromNode,
                to: toNode,
                name: "Direct",
                type: RoadSegmentType.Collector,
                lengthMeters: 390m);
            CityRoutePointDto from = CreatePoint(
                node: fromNode,
                districtId: districtId,
                name: "Origin");
            CityRoutePointDto to = CreatePoint(
                node: toNode,
                districtId: districtId,
                name: "Destination");
            var conditions = new CityRoadSegmentConditionsSnapshot(
                CityId: cityId,
                EffectiveTickId: 77,
                LastEvaluatedAtUtc: DateTimeOffset.Parse("2048-12-10T09:30:00+00:00"),
                RoadSupportIndex: 0.91m,
                Segments:
                [
                    new CityRoadSegmentConditionSnapshot(
                        RoadSegmentId: direct.Id.Value,
                        DistrictId: districtId.Value,
                        FromRoadNodeId: fromNode.Id.Value,
                        ToRoadNodeId: toNode.Id.Value,
                        Name: direct.Name,
                        Type: direct.Type.ToString(),
                        LengthMeters: direct.LengthMeters,
                        PassabilityIndex: 0.90m,
                        SpeedMultiplierIndex: 0.40m,
                        SlipRiskIndex: 0.05m,
                        ClosureRiskIndex: 0.20m,
                        MaintenancePriorityIndex: 0.30m)
                ]);

            CityRouteDto route = _planner.Plan(
                cityId: cityId,
                profile: CityRouteProfiles.Pedestrian,
                from: from,
                to: to,
                roadNodes:
                [
                    fromNode,
                    middleNode,
                    toNode
                ],
                roadSegments:
                [
                    firstHop,
                    secondHop,
                    direct
                ],
                segmentConditions: conditions);

            Assert.True(route.Accessible);
            Assert.True(route.UsedDynamicRoadConditions);
            Assert.Equal(
                expected: 77L,
                actual: route.EffectiveTickId);
            Assert.Equal(
                expected: 156m,
                actual: route.TotalDistanceMeters);
            Assert.Equal(
                expected: 2m,
                actual: route.EstimatedTravelTimeMinutes);
            Assert.Equal(
                expected: 1m,
                actual: route.OverallPassabilityIndex);
            Assert.Equal(
                expected: 2,
                actual: route.Segments.Count);
            Assert.Equal(
                expected: firstHop.Id.Value,
                actual: route.Segments[0].RoadSegmentId);
            Assert.Equal(
                expected: secondHop.Id.Value,
                actual: route.Segments[1].RoadSegmentId);
        }

        [Fact]
        public void Plan_WhenSegmentIsEffectivelyClosed_ReturnsUnreachableRoute()
        {
            var cityId = Guid.NewGuid();
            var districtId = new DistrictId(Guid.NewGuid());
            RoadNode fromNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "From",
                x: 10m,
                y: 20m);
            RoadNode toNode = CreateRoadNode(
                cityId: cityId,
                districtId: districtId,
                name: "To",
                x: 40m,
                y: 20m);
            RoadSegment direct = CreateRoadSegment(
                cityId: cityId,
                districtId: districtId,
                from: fromNode,
                to: toNode,
                name: "Direct",
                type: RoadSegmentType.Arterial,
                lengthMeters: 156m);
            CityRoutePointDto from = CreatePoint(
                node: fromNode,
                districtId: districtId,
                name: "Origin");
            CityRoutePointDto to = CreatePoint(
                node: toNode,
                districtId: districtId,
                name: "Destination");
            var conditions = new CityRoadSegmentConditionsSnapshot(
                CityId: cityId,
                EffectiveTickId: 88,
                LastEvaluatedAtUtc: DateTimeOffset.Parse("2048-12-10T10:00:00+00:00"),
                RoadSupportIndex: 0.20m,
                Segments:
                [
                    new CityRoadSegmentConditionSnapshot(
                        RoadSegmentId: direct.Id.Value,
                        DistrictId: districtId.Value,
                        FromRoadNodeId: fromNode.Id.Value,
                        ToRoadNodeId: toNode.Id.Value,
                        Name: direct.Name,
                        Type: direct.Type.ToString(),
                        LengthMeters: direct.LengthMeters,
                        PassabilityIndex: 0.17m,
                        SpeedMultiplierIndex: 0.90m,
                        SlipRiskIndex: 0.10m,
                        ClosureRiskIndex: 0.98m,
                        MaintenancePriorityIndex: 0.80m)
                ]);

            CityRouteDto route = _planner.Plan(
                cityId: cityId,
                profile: CityRouteProfiles.EmergencyResponse,
                from: from,
                to: to,
                roadNodes:
                [
                    fromNode,
                    toNode
                ],
                roadSegments: [direct],
                segmentConditions: conditions);

            Assert.False(route.Accessible);
            Assert.True(route.UsedDynamicRoadConditions);
            Assert.Equal(
                expected: 88L,
                actual: route.EffectiveTickId);
            Assert.Equal(
                expected: 0m,
                actual: route.TotalDistanceMeters);
            Assert.Equal(
                expected: 0m,
                actual: route.EstimatedTravelTimeMinutes);
            Assert.Equal(
                expected: 0m,
                actual: route.OverallPassabilityIndex);
            Assert.NotNull(route.UnreachableReason);
            Assert.Empty(route.Segments);
        }

        private static RoadNode CreateRoadNode(
            Guid cityId,
            DistrictId districtId,
            string name,
            decimal x,
            decimal y)
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

        private static CityRoutePointDto CreatePoint(
            RoadNode node,
            DistrictId districtId,
            string name)
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
}
