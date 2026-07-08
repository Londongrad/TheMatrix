namespace Matrix.Population.Contracts.Events
{
    public sealed record PopulationResidentHealthRiskV1(
        Guid ResidentId,
        int EnergyScore,
        int HappinessScore,
        int StressScore,
        int SocialNeedScore,
        bool IsVulnerable,
        string HousingStability,
        bool HasStructuredDailyActivity,
        int InfectiousHouseholdContacts,
        int HouseholdSize,
        double CaregiverSupportStrength,
        bool HadAdverseWeatherExposure,
        double HealthcareSupportStrength,
        double PublicHealthRiskStrength,
        int ExternalHealthDelta = 0,
        long LifecycleRevision = 0,
        Guid? CommunityId = null);
}
