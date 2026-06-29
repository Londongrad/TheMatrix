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
        int InfectiousHouseholdContacts,
        int HouseholdSize,
        double CaregiverSupportStrength,
        bool HadAdverseWeatherExposure,
        double HealthcareSupportStrength,
        double PublicHealthRiskStrength);
}
