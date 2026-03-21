namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityCreatedV1(
        Guid CityId,
        string Name,
        string SimulationKind,
        DateTimeOffset CreatedAtUtc,
        string? EconomyProfile = null);
}
