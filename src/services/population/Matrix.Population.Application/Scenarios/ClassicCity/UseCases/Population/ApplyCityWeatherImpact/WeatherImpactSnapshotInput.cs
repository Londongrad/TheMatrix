namespace Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed record WeatherImpactSnapshotInput(
        string Type,
        string Severity,
        string PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa);
}
