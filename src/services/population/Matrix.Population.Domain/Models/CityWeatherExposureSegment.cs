using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Domain.Models
{
    public sealed record CityWeatherExposureSegment(
        CityWeatherExposureKind Kind,
        WeatherImpactProfile Weather,
        DateTimeOffset EffectStartedAtSimTimeUtc,
        DateTimeOffset IntervalStartSimTimeUtc,
        DateTimeOffset IntervalEndSimTimeUtc,
        WeatherImpactProfile? SourceWeather = null);
}
