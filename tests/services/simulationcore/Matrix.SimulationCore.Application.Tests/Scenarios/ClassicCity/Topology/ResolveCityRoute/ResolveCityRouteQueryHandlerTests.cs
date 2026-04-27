using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology.ResolveCityRoute;

public sealed class ResolveCityRouteQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenPointsCannotBeResolved_ReturnsNull()
    {
        var cityId = new CityId(Guid.NewGuid());
        var roadNode = TopologyTestSupport.CreateRoadNode(cityId);
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
            roadNodeRepository,
            roadSegmentRepository,
            buildingRepository,
            anchorRepository,
            conditionsClient,
            planner);

        var result = await handler.Handle(
            new ResolveCityRouteQuery(
                cityId.Value,
                "road-node",
                Guid.NewGuid(),
                "city-anchor",
                Guid.NewGuid(),
                "pedestrian"),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(cityId.Value, roadNodeRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, roadSegmentRepository.RequestedCityId!.Value.Value);
        Assert.Equal(cityId.Value, conditionsClient.RequestedCityId);
        Assert.Null(planner.RequestedCityId);
    }

    [Fact]
    public async Task Handle_WhenRoutePointsResolve_ReturnsPlannerResult()
    {
        var cityId = new CityId(Guid.NewGuid());
        var district = TopologyTestSupport.CreateDistrict(cityId, "Downtown");
        var fromRoadNode = TopologyTestSupport.CreateRoadNode(cityId, district.Id, "Residence Access");
        var toRoadNode = TopologyTestSupport.CreateRoadNode(cityId, district.Id, "Hospital Access");
        var roadSegment = TopologyTestSupport.CreateRoadSegment(cityId, district.Id, fromRoadNode.Id, toRoadNode.Id, "Downtown Connector");
        var building = TopologyTestSupport.CreateResidentialBuilding(
            cityId,
            district.Id,
            name: "River Tower",
            accessRoadNodeId: fromRoadNode.Id);
        var anchor = TopologyTestSupport.CreateCityAnchor(
            cityId,
            district.Id,
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
        var expectedRoute = CreateRouteDto(cityId.Value, snapshot);
        var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository
        {
            RoadNodes = [fromRoadNode, toRoadNode]
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
            roadNodeRepository,
            roadSegmentRepository,
            buildingRepository,
            anchorRepository,
            conditionsClient,
            planner);

        var result = await handler.Handle(
            new ResolveCityRouteQuery(
                cityId.Value,
                "residential-building",
                building.Id.Value,
                "city_anchor",
                anchor.Id.Value,
                "service_vehicle"),
            CancellationToken.None);

        Assert.Same(expectedRoute, result);
        Assert.Equal(cityId.Value, planner.RequestedCityId);
        Assert.Equal("ServiceVehicle", planner.RequestedProfile);
        Assert.NotNull(planner.RequestedFrom);
        Assert.NotNull(planner.RequestedTo);
        Assert.Equal("ResidentialBuilding", planner.RequestedFrom!.Kind);
        Assert.Equal(building.Id.Value, planner.RequestedFrom.EntityId);
        Assert.Equal(building.AccessRoadNodeId.Value, planner.RequestedFrom.RoadNodeId);
        Assert.Equal("CityAnchor", planner.RequestedTo!.Kind);
        Assert.Equal(anchor.Id.Value, planner.RequestedTo.EntityId);
        Assert.Equal(anchor.AccessRoadNodeId.Value, planner.RequestedTo.RoadNodeId);
        Assert.Same(snapshot, planner.RequestedConditions);
        Assert.Equal(building.Id.Value, buildingRepository.RequestedBuildingId!.Value.Value);
        Assert.Equal(anchor.Id.Value, anchorRepository.RequestedAnchorId!.Value.Value);
    }

    private static CityRouteDto CreateRouteDto(Guid cityId, CityRoadSegmentConditionsSnapshot? snapshot = null)
    {
        return new CityRouteDto(
            CityId: cityId,
            Profile: "ServiceVehicle",
            Accessible: true,
            UsedDynamicRoadConditions: snapshot is not null,
            EffectiveTickId: snapshot?.EffectiveTickId,
            ConditionsLastEvaluatedAtUtc: snapshot?.LastEvaluatedAtUtc,
            From: new CityRoutePointDto("ResidentialBuilding", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "From", 1m, 2m),
            To: new CityRoutePointDto("CityAnchor", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "To", 3m, 4m),
            TotalDistanceMeters: 180m,
            EstimatedTravelTimeMinutes: 2.5m,
            OverallPassabilityIndex: 0.95m,
            UnreachableReason: null,
            Segments:
            [
                new CityRouteSegmentDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Segment",
                    "Collector",
                    180m,
                    2.5m,
                    0.95m,
                    0.88m,
                    0.07m,
                    0.03m)
            ]);
    }
}
