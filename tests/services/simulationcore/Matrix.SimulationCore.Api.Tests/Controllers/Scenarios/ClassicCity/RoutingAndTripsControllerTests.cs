using Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.GetCityActiveTrips;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationCore.Api.Tests.TestSupport.SimulationCoreApiTestSupport;

namespace Matrix.SimulationCore.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class RoutingAndTripsControllerTests
{
    [Fact]
    public async Task Resolve_ReturnsNotFoundOrMappedRouteView()
    {
        Guid cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
        Guid fromId = Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c");
        Guid toId = Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f");
        var missingSender = new FakeSender();
        missingSender.Handle<ResolveCityRouteQuery, CityRouteDto?>(_ => null);
        var missingController = new RoutingController(missingSender);

        IResult missing = await missingController.Resolve(cityId, CreateResolveCityRouteRequest(fromId, toId), CancellationToken.None);

        AssertStatus(missing, StatusCodes.Status404NotFound);

        var sender = new FakeSender();
        sender.Handle<ResolveCityRouteQuery, CityRouteDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            Assert.Equal("Anchor", query.FromKind);
            Assert.Equal(fromId, query.FromId);
            Assert.Equal("Building", query.ToKind);
            Assert.Equal(toId, query.ToId);
            Assert.Equal("Pedestrian", query.Profile);
            return CreateRouteDto(cityId);
        });
        var controller = new RoutingController(sender);

        IResult result = await controller.Resolve(cityId, CreateResolveCityRouteRequest(fromId, toId), CancellationToken.None);
        CityRouteView view = AssertResult<CityRouteView>(result, StatusCodes.Status200OK);

        Assert.True(view.Accessible);
        Assert.Single(view.Segments);
        Assert.Equal(240m, view.TotalDistanceMeters);
    }

    [Fact]
    public async Task DispatchAndListActive_MapStatusesAndViews()
    {
        Guid cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
        Guid fromId = Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c");
        Guid toId = Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f");
        DispatchCityTripRequest request = CreateDispatchCityTripRequest(fromId, toId);
        var sender = new FakeSender();
        sender.Handle<DispatchCityTripCommand, DispatchCityTripResult>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(request.Purpose, command.Purpose);
            Assert.Equal(request.Profile, command.Profile);
            Assert.Equal(request.TravellerEntityId, command.TravellerEntityId);
            return new DispatchCityTripResult(
                Status: DispatchCityTripStatus.Created,
                Trip: CreateActiveTripDto(cityId),
                FailureReason: null);
        });
        sender.Handle<GetCityActiveTripsQuery, IReadOnlyList<CityActiveTripDto>>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return [CreateActiveTripDto(cityId)];
        });
        var controller = new TripsController(sender);

        IResult dispatch = await controller.Dispatch(cityId, request, CancellationToken.None);
        IResult active = await controller.ListActive(cityId, CancellationToken.None);

        CityActiveTripView tripView = AssertResult<CityActiveTripView>(dispatch, StatusCodes.Status200OK);
        CityActiveTripView[] activeTrips = AssertResult<CityActiveTripView[]>(active, StatusCodes.Status200OK);
        Assert.Equal("Commuter", tripView.Subject);
        Assert.Single(activeTrips);
        Assert.Equal(0.4m, activeTrips[0].Current.SegmentProgressIndex);
    }

    [Fact]
    public async Task Dispatch_MapsNotFoundAndConflictBranches()
    {
        Guid cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
        DispatchCityTripRequest request = CreateDispatchCityTripRequest(
            fromId: Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c"),
            toId: Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f"));

        static TripsController CreateController(DispatchCityTripStatus status, string reason)
        {
            var sender = new FakeSender();
            sender.Handle<DispatchCityTripCommand, DispatchCityTripResult>(_ => new DispatchCityTripResult(status, null, reason));
            return new TripsController(sender);
        }

        IResult notFound = await CreateController(DispatchCityTripStatus.CityNotFound, "Missing").Dispatch(cityId, request, CancellationToken.None);
        IResult cityNotReady = await CreateController(DispatchCityTripStatus.CityNotReady, "City is provisioning").Dispatch(cityId, request, CancellationToken.None);
        IResult routeUnavailable = await CreateController(DispatchCityTripStatus.RouteUnavailable, "Route blocked").Dispatch(cityId, request, CancellationToken.None);

        AssertStatus(notFound, StatusCodes.Status404NotFound);
        Assert.Equal(
            "SimulationCore.World.ActiveTrip.CityNotReady",
            GetAnonymousProperty<string>(cityNotReady, "code", StatusCodes.Status409Conflict));
        Assert.Equal(
            "SimulationCore.World.ActiveTrip.RouteUnavailable",
            GetAnonymousProperty<string>(routeUnavailable, "code", StatusCodes.Status409Conflict));
    }
}
