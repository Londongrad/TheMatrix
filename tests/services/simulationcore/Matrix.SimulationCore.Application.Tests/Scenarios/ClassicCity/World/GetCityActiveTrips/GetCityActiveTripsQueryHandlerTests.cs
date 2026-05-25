using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.GetCityActiveTrips;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.GetCityActiveTrips
{
    public sealed class GetCityActiveTripsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsMappedActiveTrips()
        {
            CityActiveTrip trip = WorldTestSupport.CreateActiveTrip();
            var repository = new WorldTestSupport.FakeCityActiveTripRepository
            {
                Trips = [trip]
            };
            var handler = new GetCityActiveTripsQueryHandler(repository);

            IReadOnlyList<CityActiveTripDto> result = await handler.Handle(
                request: new GetCityActiveTripsQuery(trip.CityId.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: trip.CityId.Value,
                actual: repository.RequestedCityId!.Value.Value);
            CityActiveTripDto item = Assert.Single(result);
            Assert.Equal(
                expected: trip.Id.Value,
                actual: item.TripId);
            Assert.Equal(
                expected: trip.CityId.Value,
                actual: item.CityId);
            Assert.Equal(
                expected: trip.TravellerEntityId,
                actual: item.TravellerEntityId);
            Assert.Equal(
                expected: trip.Subject,
                actual: item.Subject);
            Assert.Equal(
                expected: "WorkCommute",
                actual: item.Purpose);
            Assert.Equal(
                expected: trip.Profile,
                actual: item.Profile);
            Assert.Equal(
                expected: "Active",
                actual: item.Status);
            Assert.Equal(
                expected: trip.MovementCapabilityIndex,
                actual: item.MovementCapabilityIndex);
            Assert.Equal(
                expected: trip.TotalDistanceMeters,
                actual: item.TotalDistanceMeters);
            Assert.Equal(
                expected: trip.DistanceTravelledMeters,
                actual: item.DistanceTravelledMeters);
            Assert.Equal(
                expected: trip.RemainingDistanceMeters,
                actual: item.RemainingDistanceMeters);
            Assert.Equal(
                expected: trip.FromName,
                actual: item.From.Name);
            Assert.Equal(
                expected: trip.ToName,
                actual: item.To.Name);
            Assert.Equal(
                expected: trip.CurrentDistrictId.Value,
                actual: item.Current.DistrictId);
            Assert.Equal(
                expected: trip.CurrentRoadSegmentId!.Value.Value,
                actual: item.Current.RoadSegmentId);
        }
    }
}
