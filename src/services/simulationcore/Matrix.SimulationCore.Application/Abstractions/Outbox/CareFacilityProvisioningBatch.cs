namespace Matrix.SimulationCore.Application.Abstractions.Outbox;

public sealed record CareFacilityProvisioningBatch(
    Guid SimulationHostId,
    long SourceRevision,
    DateTimeOffset SynchronizedAtUtc,
    string CorrelationId,
    IReadOnlyList<CareFacilityProvisioning> Facilities);
