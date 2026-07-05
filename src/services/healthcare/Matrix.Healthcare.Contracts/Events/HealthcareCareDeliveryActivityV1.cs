namespace Matrix.Healthcare.Contracts.Events;

public sealed record HealthcareCareDeliveryActivityV1(
    Guid SimulationHostId,
    long SourceRevision,
    DateOnly CareDate,
    int ProcessedPatientCount,
    int RoutineCareDeliveryCount,
    int UrgentCareDeliveryCount,
    int AcuteCareDeliveryCount,
    int EmergencyCareDeliveryCount,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId);
