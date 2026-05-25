using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.ResolveCityRoute
{
    public sealed class ResolveCityRouteQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenPointsCannotBeResolved_ReturnsNull()
        {
            var cityId = new CityId(Guid.NewGuid());
            RoadNode roadNode = TopologyTestSupport.CreateRoadNode(cityId);
            var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository
            {
                RoadNodes = [roadNode]
            };
            var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository();
            var buildingRepository = new TopologyTestSupport.FakeResidentialBuildingRepository();
            var anchorRepository = new TopologyTestSupport.FakeCityAnchorRepository();
            var conditionsClient = new TopologyTestSupport.FakeCityRoadSegmentConditionsClient();
            var planner = new TopologyTestSupport.FakeClassicCityRoutePlanner
            {
                Result = CreateRouteDto(cityId.Value)
            };
            var handler = new ResolveCityRouteQueryHandler(
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository,
                residentialBuildingRepository: buildingRepository,
                cityAnchorRepository: anchorRepository,
                roadSegmentConditionsClient: conditionsClient,
                routePlanner: planner);

            CityRouteDto? result = await handler.Handle(
                request: new ResolveCityRouteQuery(
                    CityId: cityId.Value,
                    FromKind: "road-node",
                    FromId: Guid.NewGuid(),
                    ToKind: "city-anchor",
                    ToId: Guid.NewGuid(),
                    Profile: "pedestrian"),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: cityId.Value,
                actual: roadNodeRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: roadSegmentRepository.RequestedCityId!.Value.Value);
            Assert.Equal(
                expected: cityId.Value,
                actual: conditionsClient.RequestedCityId);
            Assert.Null(planner.RequestedCityId);
        }

        [Fact]
        public async Task Handle_WhenRoutePointsResolve_ReturnsPlannerResult()
        {
            var cityId = new CityId(Guid.NewGuid());
            District district = TopologyTestSupport.CreateDistrict(
                cityId: cityId,
                name: "Downtown");
            RoadNode fromRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: cityId,
                districtId: district.Id,
                name: "Residence Access");
            RoadNode toRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: cityId,
                districtId: district.Id,
                name: "Hospital Access");
            RoadSegment roadSegment = TopologyTestSupport.CreateRoadSegment(
                cityId: cityId,
                districtId: district.Id,
                fromRoadNodeId: fromRoadNode.Id,
                toRoadNodeId: toRoadNode.Id,
                name: "Downtown Connector");
            ResidentialBuilding building = TopologyTestSupport.CreateResidentialBuilding(
                cityId: cityId,
                districtId: district.Id,
                name: "River Tower",
                accessRoadNodeId: fromRoadNode.Id);
            CityAnchor anchor = TopologyTestSupport.CreateCityAnchor(
                cityId: cityId,
                districtId: district.Id,
                name: "Central Hospital",
                accessRoadNodeId: toRoadNode.Id);
            var snapshot = new CityRoadSegmentConditionsSnapshot(
                CityId: cityId.Value,
                EffectiveTickId: 42,
                LastEvaluatedAtUtc: TopologyTestSupport.CreatedAtUtc,
                RoadSupportIndex: 0.92m,
                Segments:
                [
                    new CityRoadSegmentConditionSnapshot(
                        RoadSegmentId: roadSegment.Id.Value,
                        DistrictId: roadSegment.DistrictId.Value,
                        FromRoadNodeId: roadSegment.FromRoadNodeId.Value,
                        ToRoadNodeId: roadSegment.ToRoadNodeId.Value,
                        Name: roadSegment.Name,
                        Type: roadSegment.Type.ToString(),
                        LengthMeters: roadSegment.LengthMeters,
                        PassabilityIndex: 0.95m,
                        SpeedMultiplierIndex: 0.88m,
                        SlipRiskIndex: 0.07m,
                        ClosureRiskIndex: 0.03m,
                        MaintenancePriorityIndex: 0.15m)
                ]);
            CityRouteDto expectedRoute = CreateRouteDto(
                cityId: cityId.Value,
                snapshot: snapshot);
            var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository
            {
                RoadNodes =
                [
                    fromRoadNode,
                    toRoadNode
                ]
            };
            var roadSegmentRepository = new TopologyTestSupport.FakeRoadSegmentRepository
            {
                RoadSegments = [roadSegment]
            };
            var buildingRepository = new TopologyTestSupport.FakeResidentialBuildingRepository
            {
                BuildingById = building
            };
            var anchorRepository = new TopologyTestSupport.FakeCityAnchorRepository
            {
                AnchorById = anchor
            };
            var conditionsClient = new TopologyTestSupport.FakeCityRoadSegmentConditionsClient
            {
                Snapshot = snapshot
            };
            var planner = new TopologyTestSupport.FakeClassicCityRoutePlanner
            {
                Result = expectedRoute
            };
            var handler = new ResolveCityRouteQueryHandler(
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository,
                residentialBuildingRepository: buildingRepository,
                cityAnchorRepository: anchorRepository,
                roadSegmentConditionsClient: conditionsClient,
                routePlanner: planner);

            CityRouteDto? result = await handler.Handle(
                request: new ResolveCityRouteQuery(
                    CityId: cityId.Value,
                    FromKind: "residential-building",
                    FromId: building.Id.Value,
                    ToKind: "city_anchor",
                    ToId: anchor.Id.Value,
                    Profile: "service_vehicle"),
                cancellationToken: CancellationToken.None);

            Assert.Same(
                expected: expectedRoute,
                actual: result);
            Assert.Equal(
                expected: cityId.Value,
                actual: planner.RequestedCityId);
            Assert.Equal(
                expected: "ServiceVehicle",
                actual: planner.RequestedProfile);
            Assert.NotNull(planner.RequestedFrom);
            Assert.NotNull(planner.RequestedTo);
            Assert.Equal(
                expected: "ResidentialBuilding",
                actual: planner.RequestedFrom!.Kind);
            Assert.Equal(
                expected: building.Id.Value,
                actual: planner.RequestedFrom.EntityId);
            Assert.Equal(
                expected: building.AccessRoadNodeId.Value,
                actual: planner.RequestedFrom.RoadNodeId);
            Assert.Equal(
                expected: "CityAnchor",
                actual: planner.RequestedTo!.Kind);
            Assert.Equal(
                expected: anchor.Id.Value,
                actual: planner.RequestedTo.EntityId);
            Assert.Equal(
                expected: anchor.AccessRoadNodeId.Value,
                actual: planner.RequestedTo.RoadNodeId);
            Assert.Same(
                expected: snapshot,
                actual: planner.RequestedConditions);
            Assert.Equal(
                expected: building.Id.Value,
                actual: buildingRepository.RequestedBuildingId!.Value.Value);
            Assert.Equal(
                expected: anchor.Id.Value,
                actual: anchorRepository.RequestedAnchorId!.Value.Value);
        }

        private static CityRouteDto CreateRouteDto(
            Guid cityId,
            CityRoadSegmentConditionsSnapshot? snapshot = null)
        {
            return new CityRouteDto(
                CityId: cityId,
                Profile: "ServiceVehicle",
                Accessible: true,
                UsedDynamicRoadConditions: snapshot is not null,
                EffectiveTickId: snapshot?.EffectiveTickId,
                ConditionsLastEvaluatedAtUtc: snapshot?.LastEvaluatedAtUtc,
                From: new CityRoutePointDto(
                    Kind: "ResidentialBuilding",
                    EntityId: Guid.NewGuid(),
                    DistrictId: Guid.NewGuid(),
                    RoadNodeId: Guid.NewGuid(),
                    Name: "From",
                    PositionX: 1m,
                    PositionY: 2m),
                To: new CityRoutePointDto(
                    Kind: "CityAnchor",
                    EntityId: Guid.NewGuid(),
                    DistrictId: Guid.NewGuid(),
                    RoadNodeId: Guid.NewGuid(),
                    Name: "To",
                    PositionX: 3m,
                    PositionY: 4m),
                TotalDistanceMeters: 180m,
                EstimatedTravelTimeMinutes: 2.5m,
                OverallPassabilityIndex: 0.95m,
                UnreachableReason: null,
                Segments:
                [
                    new CityRouteSegmentDto(
                        RoadSegmentId: Guid.NewGuid(),
                        DistrictId: Guid.NewGuid(),
                        FromRoadNodeId: Guid.NewGuid(),
                        ToRoadNodeId: Guid.NewGuid(),
                        Name: "Segment",
                        Type: "Collector",
                        LengthMeters: 180m,
                        EstimatedTraversalMinutes: 2.5m,
                        PassabilityIndex: 0.95m,
                        SpeedMultiplierIndex: 0.88m,
                        SlipRiskIndex: 0.07m,
                        ClosureRiskIndex: 0.03m)
                ]);
        }
    }
}
