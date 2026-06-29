using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Population.Contracts.Events;

namespace Matrix.Healthcare.Integration.Consumers
{
    internal static class PopulationResidentHealthRiskCommandMapper
    {
        internal static AdvancePatientHealthCommand Map(PopulationResidentHealthRiskBatchV1 message)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Residents);

            if (string.IsNullOrWhiteSpace(message.CorrelationId))
                throw new ArgumentException(
                    message: "A resident health risk correlation identifier is required.",
                    paramName: nameof(message));
            if (message.TotalBatches <= 0
                || message.BatchNumber <= 0
                || message.BatchNumber > message.TotalBatches)
                throw new ArgumentException(
                    message: "Resident health risk batch position metadata is invalid.",
                    paramName: nameof(message));

            AdvancePatientHealthRiskItem[] patients = message.Residents
               .Select(resident => new AdvancePatientHealthRiskItem(
                    PatientId: resident.ResidentId,
                    EnergyScore: resident.EnergyScore,
                    HappinessScore: resident.HappinessScore,
                    StressScore: resident.StressScore,
                    SocialNeedScore: resident.SocialNeedScore,
                    IsVulnerable: resident.IsVulnerable,
                    HousingStability: MapHousingStability(resident.HousingStability),
                    HasStructuredDailyActivity: resident.HasStructuredDailyActivity,
                    InfectiousHouseholdContacts: resident.InfectiousHouseholdContacts,
                    HouseholdSize: resident.HouseholdSize,
                    CaregiverSupportStrength: resident.CaregiverSupportStrength,
                    HadAdverseWeatherExposure: resident.HadAdverseWeatherExposure,
                    HealthcareSupportStrength: resident.HealthcareSupportStrength,
                    PublicHealthRiskStrength: resident.PublicHealthRiskStrength))
               .ToArray();

            return new AdvancePatientHealthCommand(
                SimulationHostId: message.SimulationHostId,
                SourceRevision: message.SourceRevision,
                PreviousDate: message.PreviousDate,
                CurrentDate: message.CurrentDate,
                ObservedAtUtc: message.ObservedAtUtc,
                CorrelationId: message.CorrelationId,
                BatchNumber: message.BatchNumber,
                TotalBatches: message.TotalBatches,
                Patients: patients);
        }

        private static PatientHousingStability MapHousingStability(string? value)
        {
            if (string.Equals(value, "Unknown", StringComparison.OrdinalIgnoreCase))
                return PatientHousingStability.Unknown;
            if (string.Equals(value, "Housed", StringComparison.OrdinalIgnoreCase))
                return PatientHousingStability.Housed;
            if (string.Equals(value, "Unhoused", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Homeless", StringComparison.OrdinalIgnoreCase))
                return PatientHousingStability.Unhoused;

            throw new ArgumentException(
                message: $"Population housing stability '{value}' is not supported by Healthcare.",
                paramName: nameof(value));
        }
    }
}
