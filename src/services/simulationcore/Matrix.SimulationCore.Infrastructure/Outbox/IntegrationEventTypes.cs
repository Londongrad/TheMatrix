using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public static class IntegrationEventTypes
    {
        public const string CityCreatedV1 = SimulationCoreEventTypes.CityCreatedV1;
        public const string CityArchivedV1 = SimulationCoreEventTypes.CityArchivedV1;
        public const string CityDeletedV1 = SimulationCoreEventTypes.CityDeletedV1;
        public const string CityEnvironmentChangedV1 = SimulationCoreEventTypes.CityEnvironmentChangedV1;
        public const string CityTimeAdvancedV1 = SimulationCoreEventTypes.CityTimeAdvancedV1;
        public const string CityWeatherCreatedV1 = SimulationCoreEventTypes.CityWeatherCreatedV1;
        public const string CityWeatherChangedV1 = SimulationCoreEventTypes.CityWeatherChangedV1;
        public const string WeatherOverrideStartedV1 = SimulationCoreEventTypes.WeatherOverrideStartedV1;
        public const string WeatherOverrideCancelledV1 = SimulationCoreEventTypes.WeatherOverrideCancelledV1;
        public const string WeatherOverrideExpiredV1 = SimulationCoreEventTypes.WeatherOverrideExpiredV1;
        public const string ClimateProfileChangedV1 = SimulationCoreEventTypes.ClimateProfileChangedV1;
    }
}
