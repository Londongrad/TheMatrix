using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.World;
using Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Services.World
{
    public sealed class CityActiveTripAdvanceExecutorTests
    {
        [Fact]
        public async Task AdvanceAsync_WhenTargetTimeDoesNotMoveForward_ReturnsWithoutLoadingTrips()
        {
            var cityId = Guid.NewGuid();
            var repository = new WorldTestSupport.FakeCityActiveTripRepository();
            var executor = new CityActiveTripAdvanceExecutor(repository);
            DateTimeOffset fromSimTimeUtc = WorldTestSupport.StartedAtUtc;

            await executor.AdvanceAsync(
                cityId: new CityId(cityId),
                fromSimTimeUtc: fromSimTimeUtc,
                toSimTimeUtc: fromSimTimeUtc,
                tickId: 55,
                cancellationToken: CancellationToken.None);

            Assert.Null(repository.RequestedUpdateCityId);
        }

        [Fact]
        public async Task AdvanceAsync_WhenTimeMovesForward_AdvancesAllActiveTrips()
        {
            CityActiveTrip firstTrip = WorldTestSupport.CreateActiveTrip();
            CityActiveTrip secondTrip = WorldTestSupport.CreateActiveTrip(
                cityId: firstTrip.CityId,
                subject: "Afternoon supply run");
            var repository = new WorldTestSupport.FakeCityActiveTripRepository
            {
                Trips =
                [
                    firstTrip,
                    secondTrip
                ]
            };
            var executor = new CityActiveTripAdvanceExecutor(repository);
            DateTimeOffset toSimTimeUtc = WorldTestSupport.StartedAtUtc.AddMinutes(8);

            await executor.AdvanceAsync(
                cityId: firstTrip.CityId,
                fromSimTimeUtc: WorldTestSupport.StartedAtUtc,
                toSimTimeUtc: toSimTimeUtc,
                tickId: 77,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: firstTrip.CityId,
                actual: repository.RequestedUpdateCityId);
            Assert.Equal(
                expected: CityActiveTripStatus.Arrived,
                actual: firstTrip.Status);
            Assert.Equal(
                expected: CityActiveTripStatus.Arrived,
                actual: secondTrip.Status);
            Assert.Equal(
                expected: toSimTimeUtc,
                actual: firstTrip.LastAdvancedAtSimTimeUtc);
            Assert.Equal(
                expected: toSimTimeUtc,
                actual: secondTrip.LastAdvancedAtSimTimeUtc);
            Assert.Equal(
                expected: 77,
                actual: firstTrip.LastAdvancedTickId);
            Assert.Equal(
                expected: 77,
                actual: secondTrip.LastAdvancedTickId);
            Assert.Equal(
                expected: 1m,
                actual: firstTrip.ProgressIndex);
            Assert.Equal(
                expected: 1m,
                actual: secondTrip.ProgressIndex);
            Assert.Null(firstTrip.CurrentRoadSegmentId);
            Assert.Null(secondTrip.CurrentRoadSegmentId);
        }
    }
}
