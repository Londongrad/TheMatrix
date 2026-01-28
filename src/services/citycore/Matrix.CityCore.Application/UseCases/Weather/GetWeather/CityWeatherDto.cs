using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Application.UseCases.Weather.GetWeather
{
    public sealed record CityWeatherDto(
        Guid CityId,
        string ClimateZone,
        string CurrentType,
        string Severity,
        string PrecipitationKind,
        decimal TemperatureC,
        decimal HumidityPercent,
        decimal WindSpeedKph,
        decimal CloudCoveragePercent,
        decimal PressureHpa,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset ExpectedUntilUtc,
        DateTimeOffset LastEvaluatedAtUtc,
        DateTimeOffset LastTransitionAtUtc,
        CityWeatherOverrideDto? ActiveOverride)
    {
        public static CityWeatherDto FromDomain(CityWeather weather)
        {
            return new CityWeatherDto(
                CityId: weather.CityId.Value,
                ClimateZone: weather.ClimateProfile.ClimateZone.ToString(),
                CurrentType: weather.CurrentState.Type.ToString(),
                Severity: weather.CurrentState.Severity.ToString(),
                PrecipitationKind: weather.CurrentState.PrecipitationKind.ToString(),
                TemperatureC: weather.CurrentState.Temperature.Value,
                HumidityPercent: weather.CurrentState.Humidity.Value,
                WindSpeedKph: weather.CurrentState.WindSpeed.Value,
                CloudCoveragePercent: weather.CurrentState.CloudCoverage.Value,
                PressureHpa: weather.CurrentState.Pressure.Value,
                StartedAtUtc: weather.CurrentState.StartedAt.ValueUtc,
                ExpectedUntilUtc: weather.CurrentState.ExpectedUntil.ValueUtc,
                LastEvaluatedAtUtc: weather.LastEvaluatedAt.ValueUtc,
                LastTransitionAtUtc: weather.LastTransitionAt.ValueUtc,
                ActiveOverride: CityWeatherOverrideDto.FromDomain(weather.ActiveOverride));
        }
    }
}