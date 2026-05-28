namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationDeletedV1(
    Guid SimulationId,
    Guid HostId,
    string ScenarioKey,
    string HostTypeKey,
    DateTimeOffset DeletedAtUtc);
