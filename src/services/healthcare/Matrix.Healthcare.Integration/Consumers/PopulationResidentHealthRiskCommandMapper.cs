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
            ValidateBatchMetadata(
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                nameof(message));

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
                    InfectiousHouseholdContacts: 0,
                    HouseholdSize: resident.HouseholdSize,
                    CaregiverSupportStrength: resident.CaregiverSupportStrength,
                    HadAdverseWeatherExposure: resident.HadAdverseWeatherExposure,
                    HealthcareSupportStrength: resident.HealthcareSupportStrength,
                    PublicHealthRiskStrength: resident.PublicHealthRiskStrength,
                    ExternalHealthDelta: resident.ExternalHealthDelta,
                    LifecycleRevision: resident.LifecycleRevision,
                    CommunityId: resident.CommunityId))
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

        internal static AdvancePatientHealthCommand Map(
            PopulationResidentHealthRiskBatchV2 message,
            PatientHealthcareSupportPolicy healthcareSupportPolicy,
            PatientEnvironmentalHealthPolicy environmentalHealthPolicy)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Residents);
            ArgumentNullException.ThrowIfNull(healthcareSupportPolicy);
            ArgumentNullException.ThrowIfNull(environmentalHealthPolicy);
            ValidateBatchMetadata(
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                nameof(message));

            AdvancePatientHealthRiskItem[] patients = message.Residents
               .Select(resident => Map(
                    resident,
                    healthcareSupportPolicy,
                    environmentalHealthPolicy))
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

        private static AdvancePatientHealthRiskItem Map(
            PopulationResidentHealthRiskV2 resident,
            PatientHealthcareSupportPolicy healthcareSupportPolicy,
            PatientEnvironmentalHealthPolicy environmentalHealthPolicy)
        {
            ArgumentNullException.ThrowIfNull(resident);
            ArgumentNullException.ThrowIfNull(resident.Household);
            ArgumentNullException.ThrowIfNull(resident.HealthcareAccess);
            ArgumentNullException.ThrowIfNull(resident.Environment);

            PatientHousingStability housingStability = MapHousingStability(
                resident.HousingStability);
            var household = new PatientHouseholdHealthContext(
                StabilityScore: resident.Household.StabilityScore,
                AdultProviderCount: resident.Household.AdultProviderCount,
                AdultStructuredParticipantCount:
                resident.Household.AdultStructuredParticipantCount,
                FunctionalLimitationCount: resident.Household.FunctionalLimitationCount,
                HasStructuredSupport: resident.Household.HasStructuredSupport);
            var healthcareAccess = new PatientHealthcareAccessContext(
                HasPrimaryCareDestination: resident.HealthcareAccess.HasPrimaryCareDestination,
                IsPrimaryCareInCommunity: resident.HealthcareAccess.IsPrimaryCareInCommunity,
                HasRouteData: resident.HealthcareAccess.HasRouteData,
                IsRouteAccessible: resident.HealthcareAccess.IsRouteAccessible,
                RouteAccessibilityIndex: resident.HealthcareAccess.RouteAccessibilityIndex,
                RoutePassabilityIndex: resident.HealthcareAccess.RoutePassabilityIndex,
                EstimatedTravelTimeMinutes: resident.HealthcareAccess.EstimatedTravelTimeMinutes,
                HasInfrastructureData: resident.HealthcareAccess.HasInfrastructureData,
                UtilityIncidentDispatchReadinessIndex:
                resident.HealthcareAccess.UtilityIncidentDispatchReadinessIndex,
                UtilityIncidentPressureIndex:
                resident.HealthcareAccess.UtilityIncidentPressureIndex,
                UtilityIncidentCoordinationDifficultyIndex:
                resident.HealthcareAccess.UtilityIncidentCoordinationDifficultyIndex,
                UtilityIncidentRestorationPriorityIndex:
                resident.HealthcareAccess.UtilityIncidentRestorationPriorityIndex,
                PowerCoverageIndex: resident.HealthcareAccess.PowerCoverageIndex,
                WaterCoverageIndex: resident.HealthcareAccess.WaterCoverageIndex,
                HeatingCoverageIndex: resident.HealthcareAccess.HeatingCoverageIndex,
                SanitationCoverageIndex: resident.HealthcareAccess.SanitationCoverageIndex,
                HealthcareQualityIndex: resident.HealthcareAccess.HealthcareQualityIndex,
                RecoverySupportIndex: resident.HealthcareAccess.RecoverySupportIndex,
                TriagePressureIndex: resident.HealthcareAccess.TriagePressureIndex);
            var environment = new PatientEnvironmentalHealthContext(
                WaterCoverageIndex: resident.Environment.WaterCoverageIndex,
                SanitationCoverageIndex: resident.Environment.SanitationCoverageIndex,
                FloodingIndex: resident.Environment.FloodingIndex,
                UtilityContinuityIndex: resident.Environment.UtilityContinuityIndex,
                EmergencyWaterShortageRiskIndex:
                resident.Environment.EmergencyWaterShortageRiskIndex,
                FoodShortageRiskIndex: resident.Environment.FoodShortageRiskIndex,
                MedicineShortageRiskIndex: resident.Environment.MedicineShortageRiskIndex,
                EmergencyRationingEnabled: resident.Environment.EmergencyRationingEnabled);

            return new AdvancePatientHealthRiskItem(
                PatientId: resident.ResidentId,
                EnergyScore: resident.EnergyScore,
                HappinessScore: resident.HappinessScore,
                StressScore: resident.StressScore,
                SocialNeedScore: resident.SocialNeedScore,
                IsVulnerable: resident.IsVulnerable,
                HousingStability: housingStability,
                HasStructuredDailyActivity: resident.HasStructuredDailyActivity,
                InfectiousHouseholdContacts: 0,
                HouseholdSize: resident.HouseholdSize,
                CaregiverSupportStrength: resident.CaregiverSupportStrength,
                HadAdverseWeatherExposure: resident.HadAdverseWeatherExposure,
                HealthcareSupportStrength: healthcareSupportPolicy.ResolveSupportStrength(
                    functionalCapacityScore: resident.FunctionalCapacityScore,
                    isVulnerable: resident.IsVulnerable,
                    isEmployed: resident.IsEmployed,
                    housingStability: housingStability,
                    household: household,
                    healthcareAccess: healthcareAccess,
                    environment: environment),
                PublicHealthRiskStrength:
                environmentalHealthPolicy.ResolvePublicHealthRiskStrength(environment),
                ExternalHealthDelta: resident.ExternalHealthDelta,
                LifecycleRevision: resident.LifecycleRevision,
                CommunityId: resident.CommunityId);
        }

        private static void ValidateBatchMetadata(
            string correlationId,
            int batchNumber,
            int totalBatches,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException(
                    message: "A resident health risk correlation identifier is required.",
                    paramName: parameterName);
            if (totalBatches <= 0 || batchNumber <= 0 || batchNumber > totalBatches)
                throw new ArgumentException(
                    message: "Resident health risk batch position metadata is invalid.",
                    paramName: parameterName);
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
