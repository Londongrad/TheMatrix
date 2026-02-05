namespace Matrix.CityCore.Contracts.Events
{
    public sealed record CityArchivedV1(
        Guid CityId,
        DateTimeOffset ArchivedAtUtc);
}
