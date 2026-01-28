namespace Matrix.CityCore.Contracts.Cities.Requests
{
    public sealed record CreateCityRequest(
        string Name,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m);
}