namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Weather.Views
{
    public sealed record CityWeatherView(
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
        CityWeatherOverrideView? ActiveOverride);
}
