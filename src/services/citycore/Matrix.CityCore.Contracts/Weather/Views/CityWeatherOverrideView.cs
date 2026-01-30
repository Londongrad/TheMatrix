namespace Matrix.CityCore.Contracts.Weather.Views
{
    public sealed record CityWeatherOverrideView(
        Guid OverrideId,
        string Source,
        string? Reason,
        string ForcedType,
        string ForcedSeverity,
        string ForcedPrecipitationKind,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc);
}
