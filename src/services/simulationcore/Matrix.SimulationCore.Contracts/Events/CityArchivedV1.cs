namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityArchivedV1(
        Guid CityId,
        DateTimeOffset ArchivedAtUtc);
}
