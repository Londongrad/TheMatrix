using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.DispatchCityTrip
{
    public sealed class DispatchCityTripCommandHandlerSuccessTests
    {
        [Fact]
        public async Task Handle_WhenRouteIsAvailable_CreatesTripAndPersistsIt()
        {
            City city = ClassicCityTestSupport.CreateCity();
            SimulationClock clock = SimulationTestSupport.CreateClock(city.Id.Value);
            District district = TopologyTestSupport.CreateDistrict(
                cityId: city.Id,
                name: "Downtown");
            RoadNode fromRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "Residence Access");
            RoadNode toRoadNode = TopologyTestSupport.CreateRoadNode(
                cityId: city.Id,
                districtId: district.Id,
                name: "Hospital Access");
            var fromEntityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var toEntityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var roadSegmentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
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
                RoadNodes =
                [
                    fromRoadNode,
                    toRoadNode
                ]
            };
            var tripRepository = new WorldTestSupport.FakeCityActiveTripRepository();
            var mediator = new WorldTestSupport.FakeMediator
            {
                Response = route
            };
            var unitOfWork = new WorldTestSupport.FakeUnitOfWork();
            var handler = new DispatchCityTripCommandHandler(
                cityRepository: cityRepository,
                clockRepository: clockRepository,
                roadNodeRepository: roadNodeRepository,
                tripRepository: tripRepository,
                mediator: mediator,
                unitOfWork: unitOfWork);
            DispatchCityTripCommand command = WorldTestSupport.CreateDispatchCommand(
                cityId: city.Id.Value,
                fromId: fromEntityId,
                toId: toEntityId,
                purpose: "service_response",
                profile: "service_vehicle",
                subject: null);

            DispatchCityTripResult result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityTripStatus.Created,
                actual: result.Status);
            Assert.Null(result.FailureReason);
            Assert.NotNull(result.Trip);
            Assert.Equal(
                expected: city.Id.Value,
                actual: roadNodeRepository.RequestedCityId!.Value.Value);
            ResolveCityRouteQuery resolveRequest = Assert.IsType<ResolveCityRouteQuery>(mediator.Requested);
            Assert.Equal(
                expected: city.Id.Value,
                actual: resolveRequest.CityId);
            Assert.Equal(
                expected: command.FromId,
                actual: resolveRequest.FromId);
            Assert.Equal(
                expected: command.ToId,
                actual: resolveRequest.ToId);
            Assert.Equal(
                expected: command.Profile,
                actual: resolveRequest.Profile);

            CityActiveTrip? trip = tripRepository.AddedTrip;
            Assert.NotNull(trip);
            Assert.Equal(
                expected: city.Id,
                actual: trip.CityId);
            Assert.Equal(
                expected: "Service response",
                actual: trip.Subject);
            Assert.Equal(
                expected: "ServiceVehicle",
                actual: trip.Profile);
            Assert.Equal(
                expected: CityTripPurpose.ServiceResponse,
                actual: trip.Purpose);
            Assert.True(trip.UsedDynamicRoadConditions);
            Assert.Equal(
                expected: clock.TickId.Value,
                actual: trip.PlannedAtTickId);
            Assert.Equal(
                expected: clock.CurrentTime.ValueUtc,
                actual: trip.StartedAtSimTimeUtc);
            Assert.Equal(
                expected: fromEntityId,
                actual: trip.FromEntityId);
            Assert.Equal(
                expected: toEntityId,
                actual: trip.ToEntityId);
            Assert.Equal(
                expected: roadSegmentId,
                actual: trip.CurrentRoadSegmentId!.Value.Value);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCallCount);

            Assert.Equal(
                expected: trip.Id.Value,
                actual: result.Trip!.TripId);
            Assert.Equal(
                expected: "ServiceResponse",
                actual: result.Trip.Purpose);
            Assert.Equal(
                expected: "ServiceVehicle",
                actual: result.Trip.Profile);
            Assert.Equal(
                expected: "Active",
                actual: result.Trip.Status);
            Assert.Equal(
                expected: "Service response",
                actual: result.Trip.Subject);
            Assert.Equal(
                expected: route.TotalDistanceMeters,
                actual: result.Trip.TotalDistanceMeters);
        }
    }
}
