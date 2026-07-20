using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Population.Contracts.Events;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentHealthRiskCommandMapperTests
    {
        private readonly PatientEnvironmentalHealthPolicy environmentalHealthPolicy = new();

        [Fact]
        public void Map_ResidentRisk_MapsScenarioNeutralProgressionCommand()
        {
            Guid residentId = Guid.NewGuid();
            Guid communityId = Guid.NewGuid();
            PopulationResidentHealthRiskBatchV1 message = CreateMessage(
                new PopulationResidentHealthRiskV1(
                    ResidentId: residentId,
                    EnergyScore: 45,
                    HappinessScore: 38,
                    StressScore: 72,
                    SocialNeedScore: 51,
                    IsVulnerable: true,
                    HousingStability: "Homeless",
                    HasStructuredDailyActivity: false,
                    HouseholdSize: 3,
                    CaregiverSupportStrength: 0.1d,
                    HadAdverseWeatherExposure: true,
                    HealthcareSupportStrength: 0.2d,
                    PublicHealthRiskStrength: 0.4d,
                    LifecycleRevision: 3,
                    CommunityId: communityId));

            AdvancePatientHealthCommand command = PopulationResidentHealthRiskCommandMapper.Map(message);

            AdvancePatientHealthRiskItem patient = Assert.Single(command.Patients);
            Assert.Equal(message.SourceRevision, command.SourceRevision);
            Assert.Equal(message.PreviousDate, command.PreviousDate);
            Assert.Equal(residentId, patient.PatientId);
            Assert.Equal(PatientHousingStability.Unhoused, patient.HousingStability);
            Assert.Equal(0.4d, patient.PublicHealthRiskStrength);
            Assert.Equal(0, patient.InfectiousHouseholdContacts);
            Assert.Equal(3, patient.LifecycleRevision);
            Assert.Equal(communityId, patient.CommunityId);
        }

        [Fact]
        public void Map_UnknownHousingValue_ThrowsArgumentException()
        {
            PopulationResidentHealthRiskBatchV1 message = CreateMessage(
                new PopulationResidentHealthRiskV1(
                    Guid.NewGuid(), 50, 50, 50, 50, false, "Shelter", true,
                    1, 0d, false, 0d, 0d));

            Assert.Throws<ArgumentException>(() =>
                PopulationResidentHealthRiskCommandMapper.Map(message));
        }

        [Fact]
        public void Map_V2RawContext_DerivesHealthcareOwnedRiskStrengths()
        {
            Guid residentId = Guid.NewGuid();
            PopulationResidentHealthRiskV2 resident = CreateV2Resident(residentId);
            PopulationResidentHealthRiskBatchV2 message = CreateMessageV2(resident);
            var supportPolicy = new PatientHealthcareSupportPolicy(environmentalHealthPolicy);

            AdvancePatientHealthCommand command = PopulationResidentHealthRiskCommandMapper.Map(
                message,
                supportPolicy,
                environmentalHealthPolicy);

            AdvancePatientHealthRiskItem patient = Assert.Single(command.Patients);
            Assert.Equal(residentId, patient.PatientId);
            Assert.Equal(0.862d, patient.PublicHealthRiskStrength, precision: 3);
            Assert.InRange(patient.HealthcareSupportStrength, 0.1d, 0.48d);
            Assert.Equal(4, patient.LifecycleRevision);
        }

        private static PopulationResidentHealthRiskBatchV1 CreateMessage(
            params PopulationResidentHealthRiskV1[] residents)
        {
            return new PopulationResidentHealthRiskBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 21,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "health-risk:21",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents: residents);
        }

        private static PopulationResidentHealthRiskBatchV2 CreateMessageV2(
            params PopulationResidentHealthRiskV2[] residents)
        {
            return new PopulationResidentHealthRiskBatchV2(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 22,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "health-risk:22",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents: residents);
        }

        private static PopulationResidentHealthRiskV2 CreateV2Resident(Guid residentId)
        {
            return new PopulationResidentHealthRiskV2(
                ResidentId: residentId,
                EnergyScore: 45,
                HappinessScore: 38,
                StressScore: 72,
                SocialNeedScore: 51,
                IsVulnerable: true,
                FunctionalCapacityScore: 60,
                IsEmployed: true,
                HousingStability: "Housed",
                HasStructuredDailyActivity: true,
                HouseholdSize: 3,
                CaregiverSupportStrength: 0.1d,
                HadAdverseWeatherExposure: true,
                Household: new PopulationResidentHouseholdHealthContextV1(
                    StabilityScore: 0.8d,
                    AdultProviderCount: 1,
                    AdultStructuredParticipantCount: 1,
                    FunctionalLimitationCount: 1,
                    HasStructuredSupport: true),
                HealthcareAccess: new PopulationResidentHealthcareAccessContextV1(
                    HasPrimaryCareDestination: true,
                    IsPrimaryCareInCommunity: true,
                    HasRouteData: true,
                    IsRouteAccessible: true,
                    RouteAccessibilityIndex: 0.9d,
                    RoutePassabilityIndex: 0.8d,
                    EstimatedTravelTimeMinutes: 20d,
                    HasInfrastructureData: true,
                    UtilityIncidentDispatchReadinessIndex: 0.9d,
                    UtilityIncidentPressureIndex: 0.2d,
                    UtilityIncidentCoordinationDifficultyIndex: 0.1d,
                    UtilityIncidentRestorationPriorityIndex: 0.1d,
                    PowerCoverageIndex: 0.9d,
                    WaterCoverageIndex: 0.85d,
                    HeatingCoverageIndex: 0.8d,
                    SanitationCoverageIndex: 0.88d,
                    HealthcareQualityIndex: 1.1d,
                    RecoverySupportIndex: 1.1d,
                    TriagePressureIndex: 0.4d),
                Environment: new PopulationResidentEnvironmentalHealthContextV1(
                    WaterCoverageIndex: 0.6d,
                    SanitationCoverageIndex: 0.5d,
                    FloodingIndex: 0.8d,
                    UtilityContinuityIndex: 0.95d,
                    EmergencyWaterShortageRiskIndex: 0.7d,
                    FoodShortageRiskIndex: 0.9d,
                    MedicineShortageRiskIndex: 0.2d,
                    EmergencyRationingEnabled: false),
                LifecycleRevision: 4);
        }
    }
}
