namespace Matrix.CityCore.Domain.Weather.Enums
{
    /// <summary>
    ///     High-level weather classification used by the city weather aggregate.
    /// </summary>
    public enum WeatherType
    {
        Clear = 0,
        Overcast = 1,
        Rain = 2,
        Snow = 3,
        Storm = 4,
        Fog = 5,
        Windy = 6,
        Heatwave = 7,
        ColdSnap = 8
    }
}
