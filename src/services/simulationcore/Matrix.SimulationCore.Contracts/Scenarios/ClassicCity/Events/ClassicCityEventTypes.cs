namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;

public static class ClassicCityEventTypes
{
    public const string ClassicCityCreatedV1 = "simulationcore.classic-city-created.v1";
    public const string CityEnvironmentChangedV1 = "simulationcore.city-environment-changed.v1";
    public const string CityWeatherCreatedV1 = "simulationcore.city-weather-created.v1";
    public const string CityWeatherChangedV1 = "simulationcore.city-weather-changed.v1";
    public const string WeatherOverrideStartedV1 = "simulationcore.weather-override-started.v1";
    public const string WeatherOverrideCancelledV1 = "simulationcore.weather-override-cancelled.v1";
    public const string WeatherOverrideExpiredV1 = "simulationcore.weather-override-expired.v1";
    public const string ClimateProfileChangedV1 = "simulationcore.climate-profile-changed.v1";
}
