namespace Matrix.CityCore.Contracts.Events
{
    public sealed record CityDeletedV1(
        Guid CityId,
        DateTimeOffset DeletedAtUtc);
}
