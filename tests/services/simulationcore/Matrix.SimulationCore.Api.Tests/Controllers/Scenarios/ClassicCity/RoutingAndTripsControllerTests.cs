using Matrix.SimulationCore.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.GetCityActiveTrips;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationCore.Api.Tests.TestSupport.SimulationCoreApiTestSupport;

namespace Matrix.SimulationCore.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class RoutingAndTripsControllerTests
    {
        [Fact]
        public async Task Resolve_ReturnsNotFoundOrMappedRouteView()
        {
            var cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            var fromId = Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c");
            var toId = Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f");
            var missingSender = new FakeSender();
            missingSender.Handle<ResolveCityRouteQuery, CityRouteDto?>(_ => null);
            var missingController = new RoutingController(missingSender);

            IResult missing = await missingController.Resolve(
                cityId: cityId,
                request: CreateResolveCityRouteRequest(
                    fromId: fromId,
                    toId: toId),
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: missing,
                expectedStatusCode: StatusCodes.Status404NotFound);

            var sender = new FakeSender();
            sender.Handle<ResolveCityRouteQuery, CityRouteDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                Assert.Equal(
                    expected: "Anchor",
                    actual: query.FromKind);
                Assert.Equal(
                    expected: fromId,
                    actual: query.FromId);
                Assert.Equal(
                    expected: "Building",
                    actual: query.ToKind);
                Assert.Equal(
                    expected: toId,
                    actual: query.ToId);
                Assert.Equal(
                    expected: "Pedestrian",
                    actual: query.Profile);
                return CreateRouteDto(cityId);
            });
            var controller = new RoutingController(sender);

            IResult result = await controller.Resolve(
                cityId: cityId,
                request: CreateResolveCityRouteRequest(
                    fromId: fromId,
                    toId: toId),
                cancellationToken: CancellationToken.None);
            CityRouteView view = AssertResult<CityRouteView>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.True(view.Accessible);
            Assert.Single(view.Segments);
            Assert.Equal(
                expected: 240m,
                actual: view.TotalDistanceMeters);
        }

        [Fact]
        public async Task DispatchAndListActive_MapStatusesAndViews()
        {
            var cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            var fromId = Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c");
            var toId = Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f");
            DispatchCityTripRequest request = CreateDispatchCityTripRequest(
                fromId: fromId,
                toId: toId);
            var sender = new FakeSender();
            sender.Handle<DispatchCityTripCommand, DispatchCityTripResult>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: request.Purpose,
                    actual: command.Purpose);
                Assert.Equal(
                    expected: request.Profile,
                    actual: command.Profile);
                Assert.Equal(
                    expected: request.TravellerEntityId,
                    actual: command.TravellerEntityId);
                return new DispatchCityTripResult(
                    Status: DispatchCityTripStatus.Created,
                    Trip: CreateActiveTripDto(cityId),
                    FailureReason: null);
            });
            sender.Handle<GetCityActiveTripsQuery, IReadOnlyList<CityActiveTripDto>>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return [CreateActiveTripDto(cityId)];
            });
            var controller = new TripsController(sender);

            IResult dispatch = await controller.Dispatch(
                cityId: cityId,
                request: request,
                cancellationToken: CancellationToken.None);
            IResult active = await controller.ListActive(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            CityActiveTripView tripView = AssertResult<CityActiveTripView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);
            CityActiveTripView[] activeTrips = AssertResult<CityActiveTripView[]>(
                result: active,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: "Commuter",
                actual: tripView.Subject);
            Assert.Single(activeTrips);
            Assert.Equal(
                expected: 0.4m,
                actual: activeTrips[0].Current.SegmentProgressIndex);
        }

        [Fact]
        public async Task Dispatch_MapsNotFoundAndConflictBranches()
        {
            var cityId = Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            DispatchCityTripRequest request = CreateDispatchCityTripRequest(
                fromId: Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c"),
                toId: Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f"));

            static TripsController CreateController(
                DispatchCityTripStatus status,
                string reason)
            {
                var sender = new FakeSender();
                sender.Handle<DispatchCityTripCommand, DispatchCityTripResult>(_
                    => new DispatchCityTripResult(
                        Status: status,
                        Trip: null,
                        FailureReason: reason));
                return new TripsController(sender);
            }

            IResult notFound = await CreateController(
                    status: DispatchCityTripStatus.CityNotFound,
                    reason: "Missing")
               .Dispatch(
                    cityId: cityId,
                    request: request,
                    cancellationToken: CancellationToken.None);
            IResult cityNotReady = await CreateController(
                    status: DispatchCityTripStatus.CityNotReady,
                    reason: "City is provisioning")
               .Dispatch(
                    cityId: cityId,
                    request: request,
                    cancellationToken: CancellationToken.None);
            IResult routeUnavailable = await CreateController(
                    status: DispatchCityTripStatus.RouteUnavailable,
                    reason: "Route blocked")
               .Dispatch(
                    cityId: cityId,
                    request: request,
                    cancellationToken: CancellationToken.None);

            AssertStatus(
                result: notFound,
                expectedStatusCode: StatusCodes.Status404NotFound);
            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.CityNotReady",
                actual: GetAnonymousProperty<string>(
                    result: cityNotReady,
                    propertyName: "code",
                    expectedStatusCode: StatusCodes.Status409Conflict));
            Assert.Equal(
                expected: "SimulationCore.World.ActiveTrip.RouteUnavailable",
                actual: GetAnonymousProperty<string>(
                    result: routeUnavailable,
                    propertyName: "code",
                    expectedStatusCode: StatusCodes.Status409Conflict));
        }
    }
}
