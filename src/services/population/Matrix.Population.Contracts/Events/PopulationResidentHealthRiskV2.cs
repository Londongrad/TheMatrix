namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentHealthRiskV2(
        Guid ResidentId,
        int EnergyScore,
        int HappinessScore,
        int StressScore,
        int SocialNeedScore,
        bool IsVulnerable,
        int FunctionalCapacityScore,
        bool IsEmployed,
        string HousingStability,
        bool HasStructuredDailyActivity,
        int HouseholdSize,
        double CaregiverSupportStrength,
        bool HadAdverseWeatherExposure,
        PopulationResidentHouseholdHealthContextV1 Household,
        PopulationResidentHealthcareAccessContextV1 HealthcareAccess,
        PopulationResidentEnvironmentalHealthContextV1 Environment,
        int ExternalHealthDelta = 0,
        long LifecycleRevision = 0,
        Guid? CommunityId = null);
}
