using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [SimulationCoreEventTypes.SimulationCreatedV1] = typeof(SimulationCreatedV1),
                [SimulationCoreEventTypes.SimulationArchivedV1] = typeof(SimulationArchivedV1),
                [SimulationCoreEventTypes.SimulationDeletedV1] = typeof(SimulationDeletedV1),
                [SimulationCoreEventTypes.SimulationTickPhaseReachedV1] = typeof(SimulationTickPhaseReachedV1),
                [SimulationCoreEventTypes.ClassicCityCreatedV1] = typeof(ClassicCityCreatedV1),
                [SimulationCoreEventTypes.CityEnvironmentChangedV1] = typeof(CityEnvironmentChangedV1),
                [SimulationCoreEventTypes.CityWeatherCreatedV1] = typeof(CityWeatherCreatedV1),
                [SimulationCoreEventTypes.CityWeatherChangedV1] = typeof(CityWeatherChangedV1),
                [SimulationCoreEventTypes.WeatherOverrideStartedV1] = typeof(WeatherOverrideStartedV1),
                [SimulationCoreEventTypes.WeatherOverrideCancelledV1] = typeof(WeatherOverrideCancelledV1),
                [SimulationCoreEventTypes.WeatherOverrideExpiredV1] = typeof(WeatherOverrideExpiredV1),
                [SimulationCoreEventTypes.ClimateProfileChangedV1] = typeof(ClimateProfileChangedV1)
            };
    }
}
