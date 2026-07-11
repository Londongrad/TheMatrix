namespace Matrix.SimulationCore.Contracts.Events;

public sealed record SimulationEducationInstitutionProvisioningBatchV1(
    Guid SimulationHostId,
    long SourceRevision,
    DateTimeOffset SynchronizedAtUtc,
    string CorrelationId,
    int BatchNumber,
    int TotalBatches,
    IReadOnlyList<SimulationEducationInstitutionProvisioningV1> Institutions);
