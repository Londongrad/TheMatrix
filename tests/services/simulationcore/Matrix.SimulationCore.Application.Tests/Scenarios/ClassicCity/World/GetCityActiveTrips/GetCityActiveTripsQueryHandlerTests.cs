using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.GetCityActiveTrips;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World.GetCityActiveTrips;

public sealed class GetCityActiveTripsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedActiveTrips()
    {
        var trip = WorldTestSupport.CreateActiveTrip();
        var repository = new WorldTestSupport.FakeCityActiveTripRepository
        {
            Trips = [trip]
        };
        var handler = new GetCityActiveTripsQueryHandler(repository);

        var result = await handler.Handle(new GetCityActiveTripsQuery(trip.CityId.Value), CancellationToken.None);

        Assert.Equal(trip.CityId.Value, repository.RequestedCityId!.Value.Value);
        var item = Assert.Single(result);
        Assert.Equal(trip.Id.Value, item.TripId);
        Assert.Equal(trip.CityId.Value, item.CityId);
        Assert.Equal(trip.TravellerEntityId, item.TravellerEntityId);
        Assert.Equal(trip.Subject, item.Subject);
        Assert.Equal("WorkCommute", item.Purpose);
        Assert.Equal(trip.Profile, item.Profile);
        Assert.Equal("Active", item.Status);
        Assert.Equal(trip.MovementCapabilityIndex, item.MovementCapabilityIndex);
        Assert.Equal(trip.TotalDistanceMeters, item.TotalDistanceMeters);
        Assert.Equal(trip.DistanceTravelledMeters, item.DistanceTravelledMeters);
        Assert.Equal(trip.RemainingDistanceMeters, item.RemainingDistanceMeters);
        Assert.Equal(trip.FromName, item.From.Name);
        Assert.Equal(trip.ToName, item.To.Name);
        Assert.Equal(trip.CurrentDistrictId.Value, item.Current.DistrictId);
        Assert.Equal(trip.CurrentRoadSegmentId!.Value.Value, item.Current.RoadSegmentId);
    }
}
