namespace Matrix.Healthcare.Application.Care;

public sealed record CareDeliveryActivitySnapshot(
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
