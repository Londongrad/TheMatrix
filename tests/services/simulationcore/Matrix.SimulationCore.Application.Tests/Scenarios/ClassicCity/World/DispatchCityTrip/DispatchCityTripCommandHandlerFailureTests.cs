using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.DispatchCityTrip
{
    public sealed class DispatchCityTripCommandHandlerFailureTests
    {
        [Fact]
        public async Task Handle_WhenCityDoesNotExist_ReturnsCityNotFound()
        {
            var cityId = Guid.NewGuid();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository();
            var tripRepository = new WorldTestSupport.FakeCityActiveTripRepository();
            var mediator = new WorldTestSupport.FakeMediator();
            var unitOfWork = new WorldTestSupport.FakeUnitOfWork();
            var handler = new DispatchCityTripCommandHandler(
                cityRepository: cityRepository,
                clockRepository: clockRepository,
                roadNodeRepository: roadNodeRepository,
                tripRepository: tripRepository,
                mediator: mediator,
                unitOfWork: unitOfWork);

            DispatchCityTripResult result = await handler.Handle(
                request: WorldTestSupport.CreateDispatchCommand(
                    cityId: cityId,
                    fromId: Guid.NewGuid(),
                    toId: Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityTripStatus.CityNotFound,
                actual: result.Status);
            Assert.Null(result.Trip);
            Assert.Equal(
                expected: "City was not found.",
                actual: result.FailureReason);
            Assert.Null(mediator.Requested);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Null(tripRepository.AddedTrip);
        }

        [Fact]
        public async Task Handle_WhenCityIsNotReady_ReturnsCityNotReady()
        {
            City city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            DispatchCityTripCommandHandler handler = CreateHandler(
                cityRepository: cityRepository,
                clockRepository: new SimulationTestSupport.FakeSimulationClockRepository());

            DispatchCityTripResult result = await handler.Handle(
                request: WorldTestSupport.CreateDispatchCommand(
                    cityId: city.Id.Value,
                    fromId: Guid.NewGuid(),
                    toId: Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityTripStatus.CityNotReady,
                actual: result.Status);
            Assert.Equal(
                expected: "Trips can be dispatched only for active cities.",
                actual: result.FailureReason);
        }

        [Fact]
        public async Task Handle_WhenClockIsMissing_ReturnsCityNotReady()
        {
            City city = ClassicCityTestSupport.CreateCity();
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
            DispatchCityTripCommandHandler handler = CreateHandler(
                cityRepository: cityRepository,
                clockRepository: clockRepository);

            DispatchCityTripResult result = await handler.Handle(
                request: WorldTestSupport.CreateDispatchCommand(
                    cityId: city.Id.Value,
                    fromId: Guid.NewGuid(),
                    toId: Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityTripStatus.CityNotReady,
                actual: result.Status);
            Assert.Equal(
                expected: "Simulation clock is not available for this city.",
                actual: result.FailureReason);
        }

        [Fact]
        public async Task Handle_WhenRouteIsUnavailable_ReturnsRouteUnavailable()
        {
            City city = ClassicCityTestSupport.CreateCity();
            SimulationClock clock = SimulationTestSupport.CreateClock(city.Id.Value);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
            {
                ClockBySimulationId = clock
            };
            var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository();
            var tripRepository = new WorldTestSupport.FakeCityActiveTripRepository();
            var mediator = new WorldTestSupport.FakeMediator
            {
                Response = null
            };
            var unitOfWork = new WorldTestSupport.FakeUnitOfWork();
            var handler = new DispatchCityTripCommandHandler(
                cityRepository: cityRepository,
                clockRepository: clockRepository,
                roadNodeRepository: roadNodeRepository,
                tripRepository: tripRepository,
                mediator: mediator,
                unitOfWork: unitOfWork);

            DispatchCityTripResult result = await handler.Handle(
                request: WorldTestSupport.CreateDispatchCommand(
                    cityId: city.Id.Value,
                    fromId: Guid.NewGuid(),
                    toId: Guid.NewGuid()),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityTripStatus.RouteUnavailable,
                actual: result.Status);
            Assert.Equal(
                expected: "Trip route could not be resolved for the selected points.",
                actual: result.FailureReason);
            ResolveCityRouteQuery request = Assert.IsType<ResolveCityRouteQuery>(mediator.Requested);
            Assert.Equal(
                expected: city.Id.Value,
                actual: request.CityId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Null(tripRepository.AddedTrip);
        }

        [Fact]
        public async Task Handle_WhenRouteIsInaccessible_ReturnsRouteUnavailable()
        {
            City city = ClassicCityTestSupport.CreateCity();
            SimulationClock clock = SimulationTestSupport.CreateClock(city.Id.Value);
            var cityRepository = new ClassicCityTestSupport.FakeCityRepository
            {
                CityById = city
            };
            var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
            {
                ClockBySimulationId = clock
            };
            CityRouteDto inaccessibleRoute = WorldTestSupport.CreateRoute(
                cityId: city.Id.Value,
                fromDistrictId: Guid.NewGuid(),
                fromRoadNodeId: Guid.NewGuid(),
                fromEntityId: Guid.NewGuid(),
                toDistrictId: Guid.NewGuid(),
                toRoadNodeId: Guid.NewGuid(),
                toEntityId: Guid.NewGuid(),
                roadSegmentId: Guid.NewGuid(),
                accessible: false,
                unreachableReason: "Bridge closed");
            var handler = new DispatchCityTripCommandHandler(
                cityRepository: cityRepository,
                clockRepository: clockRepository,
                roadNodeRepository: new TopologyTestSupport.FakeRoadNodeRepository(),
                tripRepository: new WorldTestSupport.FakeCityActiveTripRepository(),
                mediator: new WorldTestSupport.FakeMediator
                {
                    Response = inaccessibleRoute
                },
                unitOfWork: new WorldTestSupport.FakeUnitOfWork());

            DispatchCityTripResult result = await handler.Handle(
                request: WorldTestSupport.CreateDispatchCommand(
                    cityId: city.Id.Value,
                    fromId: inaccessibleRoute.From.EntityId,
                    toId: inaccessibleRoute.To.EntityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DispatchCityTripStatus.RouteUnavailable,
                actual: result.Status);
            Assert.Equal(
                expected: "Bridge closed",
                actual: result.FailureReason);
            Assert.Null(result.Trip);
        }

        private static DispatchCityTripCommandHandler CreateHandler(
            ClassicCityTestSupport.FakeCityRepository cityRepository,
            SimulationTestSupport.FakeSimulationClockRepository clockRepository)
        {
            return new DispatchCityTripCommandHandler(
                cityRepository: cityRepository,
                clockRepository: clockRepository,
                roadNodeRepository: new TopologyTestSupport.FakeRoadNodeRepository(),
                tripRepository: new WorldTestSupport.FakeCityActiveTripRepository(),
                mediator: new WorldTestSupport.FakeMediator(),
                unitOfWork: new WorldTestSupport.FakeUnitOfWork());
        }
    }
}
