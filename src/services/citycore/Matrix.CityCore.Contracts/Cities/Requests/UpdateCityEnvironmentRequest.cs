namespace Matrix.CityCore.Contracts.Cities.Requests
{
    public sealed record UpdateCityEnvironmentRequest(
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes);
}
