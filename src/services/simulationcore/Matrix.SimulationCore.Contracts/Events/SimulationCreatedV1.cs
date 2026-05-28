namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationCreatedV1(
    Guid SimulationId,
    Guid HostId,
    string ScenarioKey,
    string HostTypeKey,
    string Seed,
    Guid RunId,
    string ModelVersion,
    Guid? ProvisioningCorrelationId,
    string State,
    DateTimeOffset CreatedAtUtc);
