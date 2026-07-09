namespace Matrix.Population.Infrastructure.Persistence.Entities;

public sealed class CityHealthcareDistrictHealthSnapshotEntity
{
    private CityHealthcareDistrictHealthSnapshotEntity()
    {
    }

    public CityHealthcareDistrictHealthSnapshotEntity(
        Guid cityId,
        Guid districtId,
        int patientCount,
        int activeIllnessCount,
        int severeIllnessCount)
    {
        CityId = cityId;
        DistrictId = districtId;
        Apply(patientCount, activeIllnessCount, severeIllnessCount);
    }

    public Guid CityId { get; private set; }
    public Guid DistrictId { get; private set; }
    public int PatientCount { get; private set; }
    public int ActiveIllnessCount { get; private set; }
    public int SevereIllnessCount { get; private set; }

    public void Apply(
        int patientCount,
        int activeIllnessCount,
        int severeIllnessCount)
    {
        PatientCount = patientCount;
        ActiveIllnessCount = activeIllnessCount;
        SevereIllnessCount = severeIllnessCount;
    }
}
