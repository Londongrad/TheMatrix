namespace Matrix.CityCore.Contracts.Events
{
    public sealed record WeatherStateV1(
        string Type,
        string Severity,
        string PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset ExpectedUntilUtc);
}
