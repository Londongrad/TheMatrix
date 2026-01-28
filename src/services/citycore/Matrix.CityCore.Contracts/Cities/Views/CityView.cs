namespace Matrix.CityCore.Contracts.Cities.Views
{
    public sealed record CityView(
        Guid CityId,
        string Name,
        string Status,
        string ClimateZone,
        string Hemisphere,
        int UtcOffsetMinutes,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ArchivedAtUtc);
}