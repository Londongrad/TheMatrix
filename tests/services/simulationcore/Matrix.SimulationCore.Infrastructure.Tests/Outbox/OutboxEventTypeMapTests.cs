using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    public sealed class OutboxEventTypeMapTests
    {
        [Fact]
        public void Map_ContainsAllSupportedSimulationCoreEventTypes()
        {
            IReadOnlyDictionary<string, Type> map = OutboxEventTypeMap.Map;

            Assert.Equal(
                expected: 14,
                actual: map.Count);
            Assert.Equal(
                expected: typeof(SimulationCreatedV1),
                actual: map[SimulationCoreEventTypes.SimulationCreatedV1]);
            Assert.Equal(
                expected: typeof(SimulationArchivedV1),
                actual: map[SimulationCoreEventTypes.SimulationArchivedV1]);
            Assert.Equal(
                expected: typeof(SimulationDeletedV1),
                actual: map[SimulationCoreEventTypes.SimulationDeletedV1]);
            Assert.Equal(
                expected: typeof(SimulationTickPhaseReachedV1),
                actual: map[SimulationCoreEventTypes.SimulationTickPhaseReachedV1]);
            Assert.Equal(
                expected: typeof(CityCreatedV1),
                actual: map[SimulationCoreEventTypes.CityCreatedV1]);
            Assert.Equal(
                expected: typeof(CityArchivedV1),
                actual: map[SimulationCoreEventTypes.CityArchivedV1]);
            Assert.Equal(
                expected: typeof(CityDeletedV1),
                actual: map[SimulationCoreEventTypes.CityDeletedV1]);
            Assert.Equal(
                expected: typeof(CityEnvironmentChangedV1),
                actual: map[SimulationCoreEventTypes.CityEnvironmentChangedV1]);
            Assert.Equal(
                expected: typeof(CityWeatherCreatedV1),
                actual: map[SimulationCoreEventTypes.CityWeatherCreatedV1]);
            Assert.Equal(
                expected: typeof(CityWeatherChangedV1),
                actual: map[SimulationCoreEventTypes.CityWeatherChangedV1]);
            Assert.Equal(
                expected: typeof(WeatherOverrideStartedV1),
                actual: map[SimulationCoreEventTypes.WeatherOverrideStartedV1]);
            Assert.Equal(
                expected: typeof(WeatherOverrideCancelledV1),
                actual: map[SimulationCoreEventTypes.WeatherOverrideCancelledV1]);
            Assert.Equal(
                expected: typeof(WeatherOverrideExpiredV1),
                actual: map[SimulationCoreEventTypes.WeatherOverrideExpiredV1]);
            Assert.Equal(
                expected: typeof(ClimateProfileChangedV1),
                actual: map[SimulationCoreEventTypes.ClimateProfileChangedV1]);
        }
    }
}
