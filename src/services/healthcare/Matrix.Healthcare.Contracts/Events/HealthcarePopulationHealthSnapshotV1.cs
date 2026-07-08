namespace Matrix.Healthcare.Contracts.Events;

public sealed record HealthcarePopulationHealthSnapshotV1(
    Guid SimulationHostId,
    long SourceRevision,
    DateOnly CurrentDate,
    int PatientCount,
    int ActiveIllnessCount,
    int SevereIllnessCount,
    decimal MedicalLoadIndex,
    decimal TriagePressureIndex,
    decimal RecoverySupportIndex,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId);
