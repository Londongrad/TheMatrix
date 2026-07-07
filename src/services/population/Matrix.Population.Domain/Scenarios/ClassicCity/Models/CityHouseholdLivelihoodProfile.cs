using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdLivelihoodProfile(
        HousingStatus? HousingStatus,
        int ResidentCount,
        int AdultProviderCount,
        int AdultStudentCount,
        int DependentCount,
        int InfantCount,
        int FunctionalLimitationCount,
        double AverageHealth,
        double AverageEnergy,
        double AverageStress,
        double StabilityScore)
    {
        public bool IsHoused =>
            HousingStatus == Enums.HousingStatus.Housed;

        public bool HasStructuredSupport =>
            AdultProviderCount > 0 ||
            AdultStudentCount > 0;
    }
}
