using Matrix.CityCore.Contracts.Events;

namespace Matrix.CityCore.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [CityCoreEventTypes.CityDeletedV1] = typeof(CityDeletedV1),
                [CityCoreEventTypes.CityEnvironmentChangedV1] = typeof(CityEnvironmentChangedV1),
                [CityCoreEventTypes.CityTimeAdvancedV1] = typeof(CityTimeAdvancedV1),
                [CityCoreEventTypes.CityWeatherCreatedV1] = typeof(CityWeatherCreatedV1),
                [CityCoreEventTypes.CityWeatherChangedV1] = typeof(CityWeatherChangedV1),
                [CityCoreEventTypes.WeatherOverrideStartedV1] = typeof(WeatherOverrideStartedV1),
                [CityCoreEventTypes.WeatherOverrideCancelledV1] = typeof(WeatherOverrideCancelledV1),
                [CityCoreEventTypes.WeatherOverrideExpiredV1] = typeof(WeatherOverrideExpiredV1),
                [CityCoreEventTypes.ClimateProfileChangedV1] = typeof(ClimateProfileChangedV1)
            };
    }
}
