namespace Matrix.CityCore.Contracts.Cities.Views
{
    public sealed record CityView(
        Guid CityId,
        string Name,
        string Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ArchivedAtUtc);
}
