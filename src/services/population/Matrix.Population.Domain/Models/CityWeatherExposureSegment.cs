namespace Matrix.Population.Domain.Models
{
    public sealed record CityWeatherExposureSegment(
        WeatherImpactProfile Weather,
        DateTimeOffset WeatherEffectiveAtSimTimeUtc,
        DateTimeOffset IntervalStartSimTimeUtc,
        DateTimeOffset IntervalEndSimTimeUtc);
}
