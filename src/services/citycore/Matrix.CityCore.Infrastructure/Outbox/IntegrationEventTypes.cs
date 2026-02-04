using Matrix.CityCore.Contracts.Events;

namespace Matrix.CityCore.Infrastructure.Outbox
{
    public static class IntegrationEventTypes
    {
        public const string CityEnvironmentChangedV1 = CityCoreEventTypes.CityEnvironmentChangedV1;
        public const string CityTimeAdvancedV1 = CityCoreEventTypes.CityTimeAdvancedV1;
        public const string CityWeatherCreatedV1 = CityCoreEventTypes.CityWeatherCreatedV1;
        public const string CityWeatherChangedV1 = CityCoreEventTypes.CityWeatherChangedV1;
        public const string WeatherOverrideStartedV1 = CityCoreEventTypes.WeatherOverrideStartedV1;
        public const string WeatherOverrideCancelledV1 = CityCoreEventTypes.WeatherOverrideCancelledV1;
        public const string WeatherOverrideExpiredV1 = CityCoreEventTypes.WeatherOverrideExpiredV1;
        public const string ClimateProfileChangedV1 = CityCoreEventTypes.ClimateProfileChangedV1;
    }
}
