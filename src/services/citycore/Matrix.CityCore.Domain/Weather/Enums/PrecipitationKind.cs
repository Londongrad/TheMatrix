namespace Matrix.CityCore.Domain.Weather.Enums
{
    /// <summary>
    ///     Describes the dominant precipitation within a weather state.
    /// </summary>
    public enum PrecipitationKind
    {
        None = 0,
        Drizzle = 1,
        Rain = 2,
        Snow = 3,
        Sleet = 4,
        Hail = 5
    }
}
