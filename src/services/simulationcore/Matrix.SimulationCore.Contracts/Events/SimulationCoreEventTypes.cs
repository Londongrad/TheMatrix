namespace Matrix.SimulationCore.Contracts.Events
{
    public static class SimulationCoreEventTypes
    {
        public const string CityCreatedV1 = "simulationcore.city-created.v1";
        public const string CityArchivedV1 = "simulationcore.city-archived.v1";
        public const string CityDeletedV1 = "simulationcore.city-deleted.v1";
        public const string CityEnvironmentChangedV1 = "simulationcore.city-environment-changed.v1";
        public const string CityTimeAdvancedV1 = "simulationcore.city-time-advanced.v1";
        public const string CityWeatherCreatedV1 = "simulationcore.city-weather-created.v1";
        public const string CityWeatherChangedV1 = "simulationcore.city-weather-changed.v1";
        public const string WeatherOverrideStartedV1 = "simulationcore.weather-override-started.v1";
        public const string WeatherOverrideCancelledV1 = "simulationcore.weather-override-cancelled.v1";
        public const string WeatherOverrideExpiredV1 = "simulationcore.weather-override-expired.v1";
        public const string ClimateProfileChangedV1 = "simulationcore.climate-profile-changed.v1";
    }
}
