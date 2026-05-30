using Matrix.SimulationCore.Contracts.Events;

namespace Matrix.SimulationCore.Infrastructure.Outbox
{
    public static class IntegrationEventTypes
    {
        public const string SimulationCreatedV1 = SimulationCoreEventTypes.SimulationCreatedV1;
        public const string SimulationArchivedV1 = SimulationCoreEventTypes.SimulationArchivedV1;
        public const string SimulationDeletedV1 = SimulationCoreEventTypes.SimulationDeletedV1;
        public const string SimulationTickPhaseReachedV1 = SimulationCoreEventTypes.SimulationTickPhaseReachedV1;
        public const string ClassicCityCreatedV1 = SimulationCoreEventTypes.ClassicCityCreatedV1;
        public const string CityEnvironmentChangedV1 = SimulationCoreEventTypes.CityEnvironmentChangedV1;
        public const string CityWeatherCreatedV1 = SimulationCoreEventTypes.CityWeatherCreatedV1;
        public const string CityWeatherChangedV1 = SimulationCoreEventTypes.CityWeatherChangedV1;
        public const string WeatherOverrideStartedV1 = SimulationCoreEventTypes.WeatherOverrideStartedV1;
        public const string WeatherOverrideCancelledV1 = SimulationCoreEventTypes.WeatherOverrideCancelledV1;
        public const string WeatherOverrideExpiredV1 = SimulationCoreEventTypes.WeatherOverrideExpiredV1;
        public const string ClimateProfileChangedV1 = SimulationCoreEventTypes.ClimateProfileChangedV1;
    }
}
