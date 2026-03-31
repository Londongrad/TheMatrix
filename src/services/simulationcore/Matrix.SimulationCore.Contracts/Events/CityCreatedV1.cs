namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityCreatedV1(
        Guid CityId,
        string Name,
        string SimulationKind,
        DateTimeOffset CreatedAtUtc,
        string DevelopmentLevel,
        string? EconomyProfile = null,
        Guid? RunId = null,
        string? SimulationSeed = null,
        string? ScenarioModelSetVersion = null);
}
