using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityWeatherExposureSegment(
        CityWeatherExposureKind Kind,
        WeatherImpactProfile Weather,
        DateTimeOffset EffectStartedAtSimTimeUtc,
        DateTimeOffset IntervalStartSimTimeUtc,
        DateTimeOffset IntervalEndSimTimeUtc,
        WeatherImpactProfile? SourceWeather = null);
}
