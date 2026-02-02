using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Domain.Models
{
    public sealed record WeatherImpactProfile(
        PopulationWeatherType Type,
        PopulationWeatherSeverity Severity,
        PopulationPrecipitationKind PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa);
}
