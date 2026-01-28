using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record WeatherStateIntegrationData(
        string Type,
        string Severity,
        string PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset ExpectedUntilUtc)
    {
        public static WeatherStateIntegrationData FromDomain(WeatherState state)
        {
            return new WeatherStateIntegrationData(
                Type: state.Type.ToString(),
                Severity: state.Severity.ToString(),
                PrecipitationKind: state.PrecipitationKind.ToString(),
                TemperatureC: state.Temperature.Value,
                HumidityPercent: state.Humidity.Value,
                WindSpeedKph: state.WindSpeed.Value,
                CloudCoveragePercent: state.CloudCoverage.Value,
                PressureHpa: state.Pressure.Value,
                StartedAtUtc: state.StartedAt.ValueUtc,
                ExpectedUntilUtc: state.ExpectedUntil.ValueUtc);
        }
    }
}