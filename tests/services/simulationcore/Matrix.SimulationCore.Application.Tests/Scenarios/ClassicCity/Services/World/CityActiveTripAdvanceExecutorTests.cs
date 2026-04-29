using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.World;

public sealed class CityActiveTripAdvanceExecutorTests
{
    [Fact]
    public async Task AdvanceAsync_WhenTargetTimeDoesNotMoveForward_ReturnsWithoutLoadingTrips()
    {
        Guid cityId = Guid.NewGuid();
        var repository = new WorldTestSupport.FakeCityActiveTripRepository();
        var executor = new CityActiveTripAdvanceExecutor(repository);
        DateTimeOffset fromSimTimeUtc = WorldTestSupport.StartedAtUtc;

        await executor.AdvanceAsync(
            new(cityId),
            fromSimTimeUtc,
            fromSimTimeUtc,
            tickId: 55,
            CancellationToken.None);

        Assert.Null(repository.RequestedUpdateCityId);
    }

    [Fact]
    public async Task AdvanceAsync_WhenTimeMovesForward_AdvancesAllActiveTrips()
    {
        var firstTrip = WorldTestSupport.CreateActiveTrip();
        var secondTrip = WorldTestSupport.CreateActiveTrip(firstTrip.CityId, "Afternoon supply run");
        var repository = new WorldTestSupport.FakeCityActiveTripRepository
        {
            Trips = [firstTrip, secondTrip]
        };
        var executor = new CityActiveTripAdvanceExecutor(repository);
        DateTimeOffset toSimTimeUtc = WorldTestSupport.StartedAtUtc.AddMinutes(8);

        await executor.AdvanceAsync(
            firstTrip.CityId,
            WorldTestSupport.StartedAtUtc,
            toSimTimeUtc,
            tickId: 77,
            CancellationToken.None);

        Assert.Equal(firstTrip.CityId, repository.RequestedUpdateCityId);
        Assert.Equal(CityActiveTripStatus.Arrived, firstTrip.Status);
        Assert.Equal(CityActiveTripStatus.Arrived, secondTrip.Status);
        Assert.Equal(toSimTimeUtc, firstTrip.LastAdvancedAtSimTimeUtc);
        Assert.Equal(toSimTimeUtc, secondTrip.LastAdvancedAtSimTimeUtc);
        Assert.Equal(77, firstTrip.LastAdvancedTickId);
        Assert.Equal(77, secondTrip.LastAdvancedTickId);
        Assert.Equal(1m, firstTrip.ProgressIndex);
        Assert.Equal(1m, secondTrip.ProgressIndex);
        Assert.Null(firstTrip.CurrentRoadSegmentId);
        Assert.Null(secondTrip.CurrentRoadSegmentId);
    }
}
