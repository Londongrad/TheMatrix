namespace Matrix.CityCore.Contracts.Events
{
    public static class CityCoreEventTypes
    {
        public const string CityArchivedV1 = "citycore.city-archived.v1";
        public const string CityDeletedV1 = "citycore.city-deleted.v1";
        public const string CityEnvironmentChangedV1 = "citycore.city-environment-changed.v1";
        public const string CityTimeAdvancedV1 = "citycore.city-time-advanced.v1";
        public const string CityWeatherCreatedV1 = "citycore.city-weather-created.v1";
        public const string CityWeatherChangedV1 = "citycore.city-weather-changed.v1";
        public const string WeatherOverrideStartedV1 = "citycore.weather-override-started.v1";
        public const string WeatherOverrideCancelledV1 = "citycore.weather-override-cancelled.v1";
        public const string WeatherOverrideExpiredV1 = "citycore.weather-override-expired.v1";
        public const string ClimateProfileChangedV1 = "citycore.climate-profile-changed.v1";
    }
}
