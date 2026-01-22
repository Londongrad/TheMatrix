namespace Matrix.CityCore.Contracts.Cities.Requests
{
    public sealed record CreateCityRequest(
        string Name,
        DateTimeOffset StartSimTimeUtc,
        decimal SpeedMultiplier = 1.0m);
}
