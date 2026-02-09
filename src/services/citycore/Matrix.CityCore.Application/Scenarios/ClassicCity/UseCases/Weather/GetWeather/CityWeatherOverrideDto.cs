using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather
{
    public sealed record CityWeatherOverrideDto(
        Guid OverrideId,
        string Source,
        string? Reason,
        string ForcedType,
        string ForcedSeverity,
        string ForcedPrecipitationKind,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc)
    {
        public static CityWeatherOverrideDto? FromDomain(WeatherOverride? weatherOverride)
        {
            if (weatherOverride is null)
                return null;

            return new CityWeatherOverrideDto(
                OverrideId: weatherOverride.Id,
                Source: weatherOverride.Source.ToString(),
                Reason: weatherOverride.Reason,
                ForcedType: weatherOverride.ForcedState.Type.ToString(),
                ForcedSeverity: weatherOverride.ForcedState.Severity.ToString(),
                ForcedPrecipitationKind: weatherOverride.ForcedState.PrecipitationKind.ToString(),
                StartsAtUtc: weatherOverride.StartsAt.ValueUtc,
                EndsAtUtc: weatherOverride.EndsAt.ValueUtc);
        }
    }
}
