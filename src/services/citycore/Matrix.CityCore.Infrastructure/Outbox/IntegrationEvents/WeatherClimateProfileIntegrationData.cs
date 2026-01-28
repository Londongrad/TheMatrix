using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Infrastructure.Outbox.IntegrationEvents
{
    public sealed record WeatherClimateProfileIntegrationData(
        string ClimateZone,
        decimal Volatility,
        string MaxOverallSeverity,
        bool SupportsThunderstorms,
        bool SupportsSnowstorms,
        bool SupportsFog,
        bool SupportsHeatwaves)
    {
        public static WeatherClimateProfileIntegrationData FromDomain(WeatherClimateProfile profile)
        {
            return new WeatherClimateProfileIntegrationData(
                ClimateZone: profile.ClimateZone.ToString(),
                Volatility: profile.Volatility.Value,
                MaxOverallSeverity: profile.ExtremeWeatherProfile.MaxOverallSeverity.ToString(),
                SupportsThunderstorms: profile.ExtremeWeatherProfile.SupportsThunderstorms,
                SupportsSnowstorms: profile.ExtremeWeatherProfile.SupportsSnowstorms,
                SupportsFog: profile.ExtremeWeatherProfile.SupportsFog,
                SupportsHeatwaves: profile.ExtremeWeatherProfile.SupportsHeatwaves);
        }
    }
}