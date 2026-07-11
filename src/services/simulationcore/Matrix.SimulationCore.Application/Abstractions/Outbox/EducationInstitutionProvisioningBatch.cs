namespace Matrix.SimulationCore.Application.Abstractions.Outbox;

public sealed record EducationInstitutionProvisioningBatch(
    Guid SimulationHostId,
    long SourceRevision,
    DateTimeOffset SynchronizedAtUtc,
    string CorrelationId,
    IReadOnlyList<EducationInstitutionProvisioning> Institutions);
