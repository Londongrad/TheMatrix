namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationCareFacilityProvisioningBatchV1(
    Guid SimulationHostId,
    long SourceRevision,
    DateTimeOffset SynchronizedAtUtc,
    string CorrelationId,
    int BatchNumber,
    int TotalBatches,
    IReadOnlyList<SimulationCareFacilityProvisioningV1> Facilities);
