namespace Matrix.Population.Infrastructure.Persistence.Entities;

public sealed class CityHealthcarePressureSnapshotEntity
{
    private CityHealthcarePressureSnapshotEntity()
    {
    }

    public CityHealthcarePressureSnapshotEntity(
        Guid cityId,
        long sourceRevision,
        DateOnly currentDate,
        int patientCount,
        int activeIllnessCount,
        int severeIllnessCount,
        decimal medicalLoadIndex,
        decimal triagePressureIndex,
        decimal recoverySupportIndex,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        CityId = cityId;
        Apply(
            sourceRevision,
            currentDate,
            patientCount,
            activeIllnessCount,
            severeIllnessCount,
            medicalLoadIndex,
            triagePressureIndex,
            recoverySupportIndex,
            occurredAtUtc,
            updatedAtUtc);
    }

    public Guid CityId { get; private set; }
    public long SourceRevision { get; private set; }
    public DateOnly CurrentDate { get; private set; }
    public int PatientCount { get; private set; }
    public int ActiveIllnessCount { get; private set; }
    public int SevereIllnessCount { get; private set; }
    public decimal MedicalLoadIndex { get; private set; }
    public decimal TriagePressureIndex { get; private set; }
    public decimal RecoverySupportIndex { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Apply(
        long sourceRevision,
        DateOnly currentDate,
        int patientCount,
        int activeIllnessCount,
        int severeIllnessCount,
        decimal medicalLoadIndex,
        decimal triagePressureIndex,
        decimal recoverySupportIndex,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        SourceRevision = sourceRevision;
        CurrentDate = currentDate;
        PatientCount = patientCount;
        ActiveIllnessCount = activeIllnessCount;
        SevereIllnessCount = severeIllnessCount;
        MedicalLoadIndex = medicalLoadIndex;
        TriagePressureIndex = triagePressureIndex;
        RecoverySupportIndex = recoverySupportIndex;
        OccurredAtUtc = occurredAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }
}
