namespace Matrix.CityCore.Contracts.Events
{
    public sealed record WeatherClimateProfileV1(
        string ClimateZone,
        decimal Volatility,
        string MaxOverallSeverity,
        bool SupportsThunderstorms,
        bool SupportsSnowstorms,
        bool SupportsFog,
        bool SupportsHeatwaves);
}
