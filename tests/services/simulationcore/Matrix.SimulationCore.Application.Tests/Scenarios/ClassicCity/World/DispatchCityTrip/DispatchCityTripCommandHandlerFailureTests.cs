using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Application.Tests.UseCases.Simulation;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.DispatchCityTrip;

public sealed class DispatchCityTripCommandHandlerFailureTests
{
    [Fact]
    public async Task Handle_WhenCityDoesNotExist_ReturnsCityNotFound()
    {
        Guid cityId = Guid.NewGuid();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository();
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var roadNodeRepository = new TopologyTestSupport.FakeRoadNodeRepository();
        var tripRepository = new WorldTestSupport.FakeCityActiveTripRepository();
        var mediator = new WorldTestSupport.FakeMediator();
        var unitOfWork = new WorldTestSupport.FakeUnitOfWork();
        var handler = new DispatchCityTripCommandHandler(
            cityRepository,
            clockRepository,
            roadNodeRepository,
            tripRepository,
            mediator,
            unitOfWork);

        var result = await handler.Handle(
            WorldTestSupport.CreateDispatchCommand(cityId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(DispatchCityTripStatus.CityNotFound, result.Status);
        Assert.Null(result.Trip);
        Assert.Equal("City was not found.", result.FailureReason);
        Assert.Null(mediator.Requested);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Null(tripRepository.AddedTrip);
    }

    [Fact]
    public async Task Handle_WhenCityIsNotReady_ReturnsCityNotReady()
    {
        var city = ClassicCityTestSupport.CreateCity(requiresPopulationBootstrap: true);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var handler = CreateHandler(cityRepository, new SimulationTestSupport.FakeSimulationClockRepository());

        var result = await handler.Handle(
            WorldTestSupport.CreateDispatchCommand(city.Id.Value, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(DispatchCityTripStatus.CityNotReady, result.Status);
        Assert.Equal("Trips can be dispatched only for active cities.", result.FailureReason);
    }

    [Fact]
    public async Task Handle_WhenClockIsMissing_ReturnsCityNotReady()
    {
        var city = ClassicCityTestSupport.CreateCity();
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository();
        var handler = CreateHandler(cityRepository, clockRepository);

        var result = await handler.Handle(
            WorldTestSupport.CreateDispatchCommand(city.Id.Value, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(DispatchCityTripStatus.CityNotReady, result.Status);
        Assert.Equal("Simulation clock is not available for this city.", result.FailureReason);
    }

    [Fact]
    public async Task Handle_WhenRouteIsUnavailable_ReturnsRouteUnavailable()
    {
        var city = ClassicCityTestSupport.CreateCity();
        var clock = SimulationTestSupport.CreateClock(city.Id.Value);
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
            cityRepository,
            clockRepository,
            roadNodeRepository,
            tripRepository,
            mediator,
            unitOfWork);

        var result = await handler.Handle(
            WorldTestSupport.CreateDispatchCommand(city.Id.Value, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(DispatchCityTripStatus.RouteUnavailable, result.Status);
        Assert.Equal("Trip route could not be resolved for the selected points.", result.FailureReason);
        var request = Assert.IsType<ResolveCityRouteQuery>(mediator.Requested);
        Assert.Equal(city.Id.Value, request.CityId);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Null(tripRepository.AddedTrip);
    }

    [Fact]
    public async Task Handle_WhenRouteIsInaccessible_ReturnsRouteUnavailable()
    {
        var city = ClassicCityTestSupport.CreateCity();
        var clock = SimulationTestSupport.CreateClock(city.Id.Value);
        var cityRepository = new ClassicCityTestSupport.FakeCityRepository
        {
            CityById = city
        };
        var clockRepository = new SimulationTestSupport.FakeSimulationClockRepository
        {
            ClockBySimulationId = clock
        };
        var inaccessibleRoute = WorldTestSupport.CreateRoute(
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
            cityRepository,
            clockRepository,
            new TopologyTestSupport.FakeRoadNodeRepository(),
            new WorldTestSupport.FakeCityActiveTripRepository(),
            new WorldTestSupport.FakeMediator { Response = inaccessibleRoute },
            new WorldTestSupport.FakeUnitOfWork());

        var result = await handler.Handle(
            WorldTestSupport.CreateDispatchCommand(city.Id.Value, inaccessibleRoute.From.EntityId, inaccessibleRoute.To.EntityId),
            CancellationToken.None);

        Assert.Equal(DispatchCityTripStatus.RouteUnavailable, result.Status);
        Assert.Equal("Bridge closed", result.FailureReason);
        Assert.Null(result.Trip);
    }

    private static DispatchCityTripCommandHandler CreateHandler(
        ClassicCityTestSupport.FakeCityRepository cityRepository,
        SimulationTestSupport.FakeSimulationClockRepository clockRepository)
    {
        return new DispatchCityTripCommandHandler(
            cityRepository,
            clockRepository,
            new TopologyTestSupport.FakeRoadNodeRepository(),
            new WorldTestSupport.FakeCityActiveTripRepository(),
            new WorldTestSupport.FakeMediator(),
            new WorldTestSupport.FakeUnitOfWork());
    }
}
