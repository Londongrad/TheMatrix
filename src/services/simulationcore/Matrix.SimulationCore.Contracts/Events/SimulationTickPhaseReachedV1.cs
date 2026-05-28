namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationTickPhaseReachedV1(
    Guid SimulationId,
    Guid HostId,
    string ScenarioKey,
    string HostTypeKey,
    string PhaseKey,
    DateTimeOffset FromSimTimeUtc,
    DateTimeOffset ToSimTimeUtc,
    long TickId,
    decimal SpeedMultiplier,
    int ModelVersion,
    string CausationId,
    string CorrelationId,
    DateTime OccurredOnUtc);
