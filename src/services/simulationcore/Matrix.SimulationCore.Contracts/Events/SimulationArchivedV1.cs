namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationArchivedV1(
    Guid SimulationId,
    Guid HostId,
    string ScenarioKey,
    string HostTypeKey,
    DateTimeOffset ArchivedAtUtc);
