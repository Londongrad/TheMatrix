namespace Matrix.Population.Application.Integration
{
    public sealed record PopulationResidentHealthRiskSnapshot(
        Guid ResidentId,
        int EnergyScore,
        int HappinessScore,
        int StressScore,
        int SocialNeedScore,
        bool IsVulnerable,
        string HousingStability,
        bool HasStructuredDailyActivity,
        int HouseholdSize,
        double CaregiverSupportStrength,
        bool HadAdverseWeatherExposure,
        double HealthcareSupportStrength = 0d,
        double PublicHealthRiskStrength = 0d,
        int ExternalHealthDelta = 0,
        long LifecycleRevision = 0,
        Guid? CommunityId = null,
        int FunctionalCapacityScore = 100,
        bool IsEmployed = false,
        PopulationResidentHouseholdHealthSnapshot? Household = null,
        PopulationResidentHealthcareAccessSnapshot? HealthcareAccess = null,
        PopulationResidentEnvironmentalHealthSnapshot? Environment = null);
}
