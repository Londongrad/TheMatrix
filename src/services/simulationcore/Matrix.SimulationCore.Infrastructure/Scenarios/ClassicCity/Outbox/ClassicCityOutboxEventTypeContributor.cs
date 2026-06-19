using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationCore.Infrastructure.Outbox;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Outbox
{
    public sealed class ClassicCityOutboxEventTypeContributor : IOutboxEventTypeContributor
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get; } =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [ClassicCityEventTypes.ClassicCityCreatedV1] = typeof(ClassicCityCreatedV1),
                [ClassicCityEventTypes.CityEnvironmentChangedV1] = typeof(CityEnvironmentChangedV1),
                [ClassicCityEventTypes.CityWeatherCreatedV1] = typeof(CityWeatherCreatedV1),
                [ClassicCityEventTypes.CityWeatherChangedV1] = typeof(CityWeatherChangedV1),
                [ClassicCityEventTypes.WeatherOverrideStartedV1] = typeof(WeatherOverrideStartedV1),
                [ClassicCityEventTypes.WeatherOverrideCancelledV1] = typeof(WeatherOverrideCancelledV1),
                [ClassicCityEventTypes.WeatherOverrideExpiredV1] = typeof(WeatherOverrideExpiredV1),
                [ClassicCityEventTypes.ClimateProfileChangedV1] = typeof(ClimateProfileChangedV1)
            };
    }
}
