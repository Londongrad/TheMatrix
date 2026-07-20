using Matrix.Healthcare.Domain.Progression;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Progression
{
    public sealed class PatientHealthcareSupportPolicyTests
    {
        private readonly PatientHealthcareSupportPolicy policy = new(
            new PatientEnvironmentalHealthPolicy());

        [Fact]
        public void ResolveSupportStrength_WhenHouseholdAndCareAccessAreStrong_ReturnsHigherSupport()
        {
            PatientHouseholdHealthContext strongHousehold = new(
                StabilityScore: 0.9d,
                AdultProviderCount: 1,
                AdultStructuredParticipantCount: 1,
                FunctionalLimitationCount: 1,
                HasStructuredSupport: true);
            PatientHouseholdHealthContext weakHousehold = new(
                StabilityScore: 0.1d,
                AdultProviderCount: 0,
                AdultStructuredParticipantCount: 0,
                FunctionalLimitationCount: 1,
                HasStructuredSupport: false);

            double highSupport = policy.ResolveSupportStrength(
                functionalCapacityScore: 60,
                isVulnerable: false,
                isEmployed: true,
                housingStability: PatientHousingStability.Housed,
                household: strongHousehold,
                healthcareAccess: CreateAccessContext() with
                {
                    HasPrimaryCareDestination = true,
                    IsPrimaryCareInCommunity = true,
                    HealthcareQualityIndex = 1.4d,
                    RecoverySupportIndex = 1.2d,
                    TriagePressureIndex = 0.4d
                },
                environment: CreateEnvironment());
            double lowSupport = policy.ResolveSupportStrength(
                functionalCapacityScore: 60,
                isVulnerable: false,
                isEmployed: false,
                housingStability: PatientHousingStability.Unhoused,
                household: weakHousehold,
                healthcareAccess: CreateAccessContext() with
                {
                    HasRouteData = true,
                    IsRouteAccessible = false,
                    RouteAccessibilityIndex = 0d,
                    RoutePassabilityIndex = 0d,
                    HealthcareQualityIndex = 0.7d,
                    RecoverySupportIndex = 0.5d,
                    TriagePressureIndex = 2.2d
                },
                environment: CreateEnvironment());

            Assert.InRange(highSupport, 0.20d, 0.48d);
            Assert.InRange(lowSupport, 0d, 0.15d);
            Assert.True(highSupport > lowSupport);
        }

        [Fact]
        public void ResolveSupportStrength_WhenInfrastructureIsFragile_ReducesSupport()
        {
            PatientHouseholdHealthContext household = new(
                StabilityScore: 0.8d,
                AdultProviderCount: 1,
                AdultStructuredParticipantCount: 0,
                FunctionalLimitationCount: 1,
                HasStructuredSupport: true);
            PatientHealthcareAccessContext stableAccess = CreateAccessContext() with
            {
                HasInfrastructureData = true,
                HasPrimaryCareDestination = true,
                IsPrimaryCareInCommunity = true,
                UtilityIncidentDispatchReadinessIndex = 0.9d,
                UtilityIncidentPressureIndex = 0.2d,
                UtilityIncidentCoordinationDifficultyIndex = 0.1d,
                UtilityIncidentRestorationPriorityIndex = 0.1d
            };
            PatientHealthcareAccessContext fragileAccess = stableAccess with
            {
                UtilityIncidentDispatchReadinessIndex = 0.2d,
                UtilityIncidentPressureIndex = 0.9d,
                UtilityIncidentCoordinationDifficultyIndex = 0.8d,
                UtilityIncidentRestorationPriorityIndex = 0.7d
            };

            double stableSupport = ResolveLimitedResidentSupport(household, stableAccess);
            double fragileSupport = ResolveLimitedResidentSupport(household, fragileAccess);

            Assert.True(fragileSupport < stableSupport);
        }

        [Fact]
        public void ResolveSupportStrength_WhenMedicineSupplyFalls_ReducesSupport()
        {
            PatientHouseholdHealthContext household = new(
                StabilityScore: 0.8d,
                AdultProviderCount: 1,
                AdultStructuredParticipantCount: 0,
                FunctionalLimitationCount: 1,
                HasStructuredSupport: true);
            PatientEnvironmentalHealthContext shortage = CreateEnvironment() with
            {
                MedicineShortageRiskIndex = 0.8d,
                UtilityContinuityIndex = 0.6d,
                EmergencyRationingEnabled = true
            };

            double suppliedSupport = ResolveLimitedResidentSupport(
                household,
                CreateAccessContext(),
                CreateEnvironment());
            double shortageSupport = ResolveLimitedResidentSupport(
                household,
                CreateAccessContext(),
                shortage);

            Assert.True(shortageSupport < suppliedSupport);
        }

        private double ResolveLimitedResidentSupport(
            PatientHouseholdHealthContext household,
            PatientHealthcareAccessContext access,
            PatientEnvironmentalHealthContext? environment = null)
        {
            return policy.ResolveSupportStrength(
                functionalCapacityScore: 30,
                isVulnerable: true,
                isEmployed: false,
                housingStability: PatientHousingStability.Housed,
                household: household,
                healthcareAccess: access,
                environment: environment ?? CreateEnvironment());
        }

        private static PatientHealthcareAccessContext CreateAccessContext()
        {
            return new PatientHealthcareAccessContext(
                HasPrimaryCareDestination: false,
                IsPrimaryCareInCommunity: false,
                HasRouteData: false,
                IsRouteAccessible: true,
                RouteAccessibilityIndex: 1d,
                RoutePassabilityIndex: 1d,
                EstimatedTravelTimeMinutes: null,
                HasInfrastructureData: false,
                UtilityIncidentDispatchReadinessIndex: 1d,
                UtilityIncidentPressureIndex: 0d,
                UtilityIncidentCoordinationDifficultyIndex: 0d,
                UtilityIncidentRestorationPriorityIndex: 0d,
                PowerCoverageIndex: 1d,
                WaterCoverageIndex: 1d,
                HeatingCoverageIndex: 1d,
                SanitationCoverageIndex: 1d,
                HealthcareQualityIndex: 1d,
                RecoverySupportIndex: 1d,
                TriagePressureIndex: 0d);
        }

        private static PatientEnvironmentalHealthContext CreateEnvironment()
        {
            return new PatientEnvironmentalHealthContext(
                WaterCoverageIndex: 1d,
                SanitationCoverageIndex: 1d,
                FloodingIndex: 0d,
                UtilityContinuityIndex: 1d,
                EmergencyWaterShortageRiskIndex: 0d,
                FoodShortageRiskIndex: 0d,
                MedicineShortageRiskIndex: 0d,
                EmergencyRationingEnabled: false);
        }
    }
}
