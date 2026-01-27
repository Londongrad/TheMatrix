namespace Matrix.CityCore.Domain.Weather.Enums
{
    /// <summary>
    ///     Source that initiated a forced weather override.
    /// </summary>
    public enum WeatherOverrideSource
    {
        Manual = 0,
        Scenario = 1,
        System = 2,
        Debug = 3
    }
}
