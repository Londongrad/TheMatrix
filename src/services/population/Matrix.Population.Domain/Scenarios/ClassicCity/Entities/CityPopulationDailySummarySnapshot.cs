using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationDailySummarySnapshot
    {
        private CityPopulationDailySummarySnapshot() { }

        private CityPopulationDailySummarySnapshot(
            CityId cityId,
            DateOnly snapshotDate,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            SnapshotDate = snapshotDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public DateOnly SnapshotDate { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public int HouseholdCount { get; private set; }
        public int HousedHouseholdCount { get; private set; }
        public int HomelessHouseholdCount { get; private set; }

        public int ResidentCount { get; private set; }
        public int DeceasedCount { get; private set; }
        public int HousedResidentCount { get; private set; }
        public int HomelessResidentCount { get; private set; }

        public int ChildCount { get; private set; }
        public int YouthCount { get; private set; }
        public int AdultCount { get; private set; }
        public int SeniorCount { get; private set; }

        public int EmployedCount { get; private set; }
        public int StudentCount { get; private set; }
        public int UnemployedCount { get; private set; }
        public int RetiredCount { get; private set; }

        public decimal? AverageHealth { get; private set; }
        public decimal? AverageHappiness { get; private set; }
        public decimal? AverageEnergy { get; private set; }
        public decimal? AverageStress { get; private set; }
        public decimal? AverageSocialNeed { get; private set; }
        public int ActiveIllnessCount { get; private set; }
        public int SevereIllnessCount { get; private set; }
        public decimal? MedicalLoadIndex { get; private set; }
        public decimal? TriagePressureIndex { get; private set; }
        public decimal? RecoverySupportIndex { get; private set; }
        public decimal? WorkforceAttendanceIndex { get; private set; }
        public decimal? WorkforceProductivityIndex { get; private set; }
        public decimal? StudentAttendanceIndex { get; private set; }

        public static CityPopulationDailySummarySnapshot Create(
            CityId cityId,
            DateOnly snapshotDate,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationDailySummarySnapshot(
                cityId: cityId,
                snapshotDate: snapshotDate,
                updatedAtUtc: updatedAtUtc);
        }

        public void Refresh(
            DateTimeOffset updatedAtUtc,
            int householdCount,
            int housedHouseholdCount,
            int homelessHouseholdCount,
            int residentCount,
            int deceasedCount,
            int housedResidentCount,
            int homelessResidentCount,
            int childCount,
            int youthCount,
            int adultCount,
            int seniorCount,
            int employedCount,
            int studentCount,
            int unemployedCount,
            int retiredCount,
            decimal? averageHealth,
            decimal? averageHappiness,
            decimal? averageEnergy,
            decimal? averageStress,
            decimal? averageSocialNeed,
            int activeIllnessCount,
            int severeIllnessCount,
            decimal? medicalLoadIndex,
            decimal? triagePressureIndex,
            decimal? recoverySupportIndex,
            decimal? workforceAttendanceIndex,
            decimal? workforceProductivityIndex,
            decimal? studentAttendanceIndex)
        {
            UpdatedAtUtc = updatedAtUtc;

            HouseholdCount = householdCount;
            HousedHouseholdCount = housedHouseholdCount;
            HomelessHouseholdCount = homelessHouseholdCount;

            ResidentCount = residentCount;
            DeceasedCount = deceasedCount;
            HousedResidentCount = housedResidentCount;
            HomelessResidentCount = homelessResidentCount;

            ChildCount = childCount;
            YouthCount = youthCount;
            AdultCount = adultCount;
            SeniorCount = seniorCount;

            EmployedCount = employedCount;
            StudentCount = studentCount;
            UnemployedCount = unemployedCount;
            RetiredCount = retiredCount;

            AverageHealth = averageHealth;
            AverageHappiness = averageHappiness;
            AverageEnergy = averageEnergy;
            AverageStress = averageStress;
            AverageSocialNeed = averageSocialNeed;
            ActiveIllnessCount = activeIllnessCount;
            SevereIllnessCount = severeIllnessCount;
            MedicalLoadIndex = medicalLoadIndex;
            TriagePressureIndex = triagePressureIndex;
            RecoverySupportIndex = recoverySupportIndex;
            WorkforceAttendanceIndex = workforceAttendanceIndex;
            WorkforceProductivityIndex = workforceProductivityIndex;
            StudentAttendanceIndex = studentAttendanceIndex;
        }
    }
}
