namespace Matrix.CityCore.Infrastructure.Outbox
{
    public static class IntegrationEventTypes
    {
        public const string CityTimeAdvancedV1 = "citycore.city-time-advanced.v1";
        public const string CityWeatherCreatedV1 = "citycore.city-weather-created.v1";
        public const string CityWeatherChangedV1 = "citycore.city-weather-changed.v1";
        public const string WeatherOverrideStartedV1 = "citycore.weather-override-started.v1";
        public const string WeatherOverrideCancelledV1 = "citycore.weather-override-cancelled.v1";
        public const string WeatherOverrideExpiredV1 = "citycore.weather-override-expired.v1";
        public const string ClimateProfileChangedV1 = "citycore.climate-profile-changed.v1";
    }
}
