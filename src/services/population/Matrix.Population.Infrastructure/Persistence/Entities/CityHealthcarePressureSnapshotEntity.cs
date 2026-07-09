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
        DateTimeOffset updatedAtUtc,
        IReadOnlyCollection<CityHealthcareDistrictHealthSnapshotEntity> districts)
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
            updatedAtUtc,
            districts);
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
    public ICollection<CityHealthcareDistrictHealthSnapshotEntity> Districts { get; private set; } =
        new List<CityHealthcareDistrictHealthSnapshotEntity>();

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
        DateTimeOffset updatedAtUtc,
        IReadOnlyCollection<CityHealthcareDistrictHealthSnapshotEntity> districts)
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
        SynchronizeDistricts(districts);
    }

    private void SynchronizeDistricts(
        IReadOnlyCollection<CityHealthcareDistrictHealthSnapshotEntity> districts)
    {
        ArgumentNullException.ThrowIfNull(districts);
        HashSet<Guid> incomingDistrictIds = districts
           .Select(district => district.DistrictId)
           .ToHashSet();

        foreach (CityHealthcareDistrictHealthSnapshotEntity obsolete in Districts
                    .Where(district => !incomingDistrictIds.Contains(district.DistrictId))
                    .ToArray())
            Districts.Remove(obsolete);

        foreach (CityHealthcareDistrictHealthSnapshotEntity incoming in districts)
        {
            CityHealthcareDistrictHealthSnapshotEntity? existing = Districts
               .SingleOrDefault(district => district.DistrictId == incoming.DistrictId);
            if (existing is null)
            {
                Districts.Add(incoming);
                continue;
            }

            existing.Apply(
                incoming.PatientCount,
                incoming.ActiveIllnessCount,
                incoming.SevereIllnessCount);
        }
    }
}
