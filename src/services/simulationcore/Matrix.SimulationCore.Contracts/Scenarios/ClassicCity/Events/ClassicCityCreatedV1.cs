namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;

public sealed record ClassicCityCreatedV1(
    Guid SimulationId,
    Guid HostId,
    string ScenarioKey,
    string HostTypeKey,
    string Name,
    DateTimeOffset CreatedAtUtc,
    string DevelopmentLevel,
    string EconomyProfile,
    Guid RunId,
    string SimulationSeed,
    string ScenarioModelSetVersion);
