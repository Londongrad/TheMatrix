using MediatR;

namespace Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed record ApplyCityWeatherImpactCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc,
        string WeatherType,
        string WeatherSeverity,
        string PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa) : IRequest<ApplyCityWeatherImpactResult>;
}
