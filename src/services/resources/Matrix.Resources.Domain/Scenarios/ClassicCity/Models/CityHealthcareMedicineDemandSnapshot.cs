namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

public sealed record CityHealthcareMedicineDemandSnapshot(
    int ProcessedPatientCount,
    int RoutineCareDeliveryCount,
    int UrgentCareDeliveryCount,
    int AcuteCareDeliveryCount,
    int EmergencyCareDeliveryCount,
    decimal MedicineLoadIndex,
    long SourceRevision,
    DateOnly CareDate,
    DateTimeOffset ObservedAtUtc);
