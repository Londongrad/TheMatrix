using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.DispatchCityTrip;

public sealed class DispatchCityTripCommandHandlerSuccessTests
{
    [Fact]
    public async Task Handle_WhenRouteIsAvailable_CreatesTripAndPersistsIt()
    {
        var city = ClassicCityTestSupport.CreateCity();
        var clock = SimulationTestSupport.CreateClock(city.Id.Value);
        var district = TopologyTestSupport.CreateDistrict(city.Id, "Downtown");
        var fromRoadNode = TopologyTestSupport.CreateRoadNode(city.Id, district.Id, "Residence Access");
        var toRoadNode = TopologyTestSupport.CreateRoadNode(city.Id, district.Id, "Hospital Access");
        Guid fromEntityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid toEntityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid roadSegmentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        CityRouteDto route = WorldTestSupport.CreateRoute(
            cityId: city.Id.Value,
            fromDistrictId: district.Id.Value,
            fromRoadNodeId: fromRoadNode.Id.Value,
            fromEntityId: fromEntityId,
            toDistrictId: district.Id.Value,
            toRoadNodeId: toRoadNode.Id.Value,
            toEntityId: toEntityId,
            roadSegmentId: roadSegmentId);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
        {
            ClockBySimulationId = clock
        };
        var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository
        {
            RoadNodes = [fromRoadNode, toRoadNode]
        };
        var tripRepository = new WorldTestSupport.FakeCityActiveTripRepository();
        var mediator = new WorldTestSupport.FakeMediator
        {
            Response = route
        };
        var unitOfWork = new WorldTestSupport.FakeUnitOfWork();
        var handler = new DispatchCityTripCommandHandler(
            cityRepository,
            clockRepository,
            roadNodeRepository,
            tripRepository,
            mediator,
            unitOfWork);
        var command = WorldTestSupport.CreateDispatchCommand(
            cityId: city.Id.Value,
            fromId: fromEntityId,
            toId: toEntityId,
            purpose: "service_response",
            profile: "service_vehicle",
            subject: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(DispatchCityTripStatus.Created, result.Status);
        Assert.Null(result.FailureReason);
        Assert.NotNull(result.Trip);
        Assert.Equal(city.Id.Value, roadNodeRepository.RequestedCityId!.Value.Value);
        var resolveRequest = Assert.IsType<ResolveCityRouteQuery>(mediator.Requested);
        Assert.Equal(city.Id.Value, resolveRequest.CityId);
        Assert.Equal(command.FromId, resolveRequest.FromId);
        Assert.Equal(command.ToId, resolveRequest.ToId);
        Assert.Equal(command.Profile, resolveRequest.Profile);

        var trip = tripRepository.AddedTrip;
        Assert.NotNull(trip);
        Assert.Equal(city.Id, trip.CityId);
        Assert.Equal("Service response", trip.Subject);
        Assert.Equal("ServiceVehicle", trip.Profile);
        Assert.Equal(Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums.CityTripPurpose.ServiceResponse, trip.Purpose);
        Assert.True(trip.UsedDynamicRoadConditions);
        Assert.Equal(clock.TickId.Value, trip.PlannedAtTickId);
        Assert.Equal(clock.CurrentTime.ValueUtc, trip.StartedAtSimTimeUtc);
        Assert.Equal(fromEntityId, trip.FromEntityId);
        Assert.Equal(toEntityId, trip.ToEntityId);
        Assert.Equal(roadSegmentId, trip.CurrentRoadSegmentId!.Value.Value);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        Assert.Equal(trip.Id.Value, result.Trip!.TripId);
        Assert.Equal("ServiceResponse", result.Trip.Purpose);
        Assert.Equal("ServiceVehicle", result.Trip.Profile);
        Assert.Equal("Active", result.Trip.Status);
        Assert.Equal("Service response", result.Trip.Subject);
        Assert.Equal(route.TotalDistanceMeters, result.Trip.TotalDistanceMeters);
    }
}
