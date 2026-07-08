using Matrix.Healthcare.Domain.Progression;

namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed record AdvancePatientHealthRiskItem(
        Guid PatientId,
        int EnergyScore,
        int HappinessScore,
        int StressScore,
        int SocialNeedScore,
        bool IsVulnerable,
        PatientHousingStability HousingStability,
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
