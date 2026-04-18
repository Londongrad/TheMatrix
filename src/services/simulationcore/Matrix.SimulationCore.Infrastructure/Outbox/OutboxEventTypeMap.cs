using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [SimulationCoreEventTypes.CityCreatedV1] = typeof(CityCreatedV1),
                [SimulationCoreEventTypes.CityArchivedV1] = typeof(CityArchivedV1),
                [SimulationCoreEventTypes.CityDeletedV1] = typeof(CityDeletedV1),
                [SimulationCoreEventTypes.CityEnvironmentChangedV1] = typeof(CityEnvironmentChangedV1),
                [SimulationCoreEventTypes.CityTimeAdvancedV1] = typeof(CityTimeAdvancedV1),
                [SimulationCoreEventTypes.CityTickPhaseReachedV1] = typeof(CityTickPhaseReachedV1),
                [SimulationCoreEventTypes.CityWeatherCreatedV1] = typeof(CityWeatherCreatedV1),
                [SimulationCoreEventTypes.CityWeatherChangedV1] = typeof(CityWeatherChangedV1),
                [SimulationCoreEventTypes.WeatherOverrideStartedV1] = typeof(WeatherOverrideStartedV1),
                [SimulationCoreEventTypes.WeatherOverrideCancelledV1] = typeof(WeatherOverrideCancelledV1),
                [SimulationCoreEventTypes.WeatherOverrideExpiredV1] = typeof(WeatherOverrideExpiredV1),
                [SimulationCoreEventTypes.ClimateProfileChangedV1] = typeof(ClimateProfileChangedV1)
            };
    }
}
