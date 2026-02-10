namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationSummaryWeatherDto(
        string CurrentType,
        string CurrentSeverity,
        bool IsRecoveryActive,
        string CurrentWeatherEffectiveAtSimTimeUtc,
        string LastWeatherOccurredOnUtc,
        string LastExposureProcessedAtSimTimeUtc,
        string? LastWeatherImpactAppliedAtSimTimeUtc);
}
