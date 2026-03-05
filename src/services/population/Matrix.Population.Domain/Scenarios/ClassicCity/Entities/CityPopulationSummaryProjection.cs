using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationSummaryProjection
    {
        private CityPopulationSummaryProjection() { }

        private CityPopulationSummaryProjection(
            CityId cityId,
            DateOnly currentDate,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            CurrentDate = currentDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public DateOnly CurrentDate { get; private set; }
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

        public static CityPopulationSummaryProjection Create(
            CityId cityId,
            DateOnly currentDate,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationSummaryProjection(
                cityId: cityId,
                currentDate: currentDate,
                updatedAtUtc: updatedAtUtc);
        }

        public void Refresh(
            DateOnly currentDate,
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
            decimal? averageSocialNeed)
        {
            CurrentDate = currentDate;
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
        }
    }
}
