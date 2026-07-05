using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;

public sealed class CityHealthcareMedicineDemandState
{
    private CityHealthcareMedicineDemandState() { }

    public int ProcessedPatientCount { get; private set; }
    public int RoutineCareDeliveryCount { get; private set; }
    public int UrgentCareDeliveryCount { get; private set; }
    public int AcuteCareDeliveryCount { get; private set; }
    public int EmergencyCareDeliveryCount { get; private set; }
    public decimal MedicineLoadIndex { get; private set; }
    public long? SourceRevision { get; private set; }
    public DateOnly? CareDate { get; private set; }
    public DateTimeOffset? ObservedAtUtc { get; private set; }

    public static CityHealthcareMedicineDemandState None()
    {
        return new CityHealthcareMedicineDemandState();
    }

    public bool CanApply(long sourceRevision)
    {
        return sourceRevision >= 0
               && (!SourceRevision.HasValue || sourceRevision > SourceRevision.Value);
    }

    public void ApplySnapshot(CityHealthcareMedicineDemandSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!CanApply(snapshot.SourceRevision))
            throw new InvalidOperationException(
                "Healthcare medicine demand revisions must advance monotonically.");

        ProcessedPatientCount = snapshot.ProcessedPatientCount;
        RoutineCareDeliveryCount = snapshot.RoutineCareDeliveryCount;
        UrgentCareDeliveryCount = snapshot.UrgentCareDeliveryCount;
        AcuteCareDeliveryCount = snapshot.AcuteCareDeliveryCount;
        EmergencyCareDeliveryCount = snapshot.EmergencyCareDeliveryCount;
        MedicineLoadIndex = snapshot.MedicineLoadIndex;
        SourceRevision = snapshot.SourceRevision;
        CareDate = snapshot.CareDate;
        ObservedAtUtc = snapshot.ObservedAtUtc;
    }
}
