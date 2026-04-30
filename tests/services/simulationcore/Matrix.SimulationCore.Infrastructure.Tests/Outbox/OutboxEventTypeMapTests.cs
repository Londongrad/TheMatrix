using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class OutboxEventTypeMapTests
{
    [Fact]
    public void Map_ContainsAllSupportedSimulationCoreEventTypes()
    {
        IReadOnlyDictionary<string, Type> map = OutboxEventTypeMap.Map;

        Assert.Equal(12, map.Count);
        Assert.Equal(typeof(CityCreatedV1), map[SimulationCoreEventTypes.CityCreatedV1]);
        Assert.Equal(typeof(CityArchivedV1), map[SimulationCoreEventTypes.CityArchivedV1]);
        Assert.Equal(typeof(CityDeletedV1), map[SimulationCoreEventTypes.CityDeletedV1]);
        Assert.Equal(typeof(CityEnvironmentChangedV1), map[SimulationCoreEventTypes.CityEnvironmentChangedV1]);
        Assert.Equal(typeof(CityTimeAdvancedV1), map[SimulationCoreEventTypes.CityTimeAdvancedV1]);
        Assert.Equal(typeof(CityTickPhaseReachedV1), map[SimulationCoreEventTypes.CityTickPhaseReachedV1]);
        Assert.Equal(typeof(CityWeatherCreatedV1), map[SimulationCoreEventTypes.CityWeatherCreatedV1]);
        Assert.Equal(typeof(CityWeatherChangedV1), map[SimulationCoreEventTypes.CityWeatherChangedV1]);
        Assert.Equal(typeof(WeatherOverrideStartedV1), map[SimulationCoreEventTypes.WeatherOverrideStartedV1]);
        Assert.Equal(typeof(WeatherOverrideCancelledV1), map[SimulationCoreEventTypes.WeatherOverrideCancelledV1]);
        Assert.Equal(typeof(WeatherOverrideExpiredV1), map[SimulationCoreEventTypes.WeatherOverrideExpiredV1]);
        Assert.Equal(typeof(ClimateProfileChangedV1), map[SimulationCoreEventTypes.ClimateProfileChangedV1]);
    }
}
