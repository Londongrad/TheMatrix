using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.Population.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentHealthRiskV2ConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsPatientHealthProgressionCommand()
        {
            var mediator = new HealthcareIntegrationMediatorStub();
            var environmentalPolicy = new PatientEnvironmentalHealthPolicy();
            var consumer = new PopulationResidentHealthRiskV2Consumer(
                mediator,
                new PatientHealthcareSupportPolicy(environmentalPolicy),
                environmentalPolicy,
                NullLogger<PopulationResidentHealthRiskV2Consumer>.Instance);
            Guid residentId = Guid.NewGuid();
            PopulationResidentHealthRiskBatchV2 message = CreateMessage(residentId);

            await consumer.ConsumeAsync(message, CancellationToken.None);

            AdvancePatientHealthCommand command = Assert.Single(mediator.HealthProgressionCommands);
            Assert.Equal(23, command.SourceRevision);
            Assert.Equal(residentId, Assert.Single(command.Patients).PatientId);
        }

        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                "healthcare-population-resident-health-risk-v2",
                PopulationResidentHealthRiskV2ConsumerDefinition.EndpointNameValue);
            Assert.Equal(4, PopulationResidentHealthRiskV2ConsumerDefinition.ConcurrentMessageLimitValue);
        }

        private static PopulationResidentHealthRiskBatchV2 CreateMessage(Guid residentId)
        {
            var neutralHousehold = new PopulationResidentHouseholdHealthContextV1(
                StabilityScore: 0.5d,
                AdultProviderCount: 1,
                AdultStructuredParticipantCount: 0,
                FunctionalLimitationCount: 0,
                HasStructuredSupport: true);
            var neutralAccess = new PopulationResidentHealthcareAccessContextV1(
                HasPrimaryCareDestination: true,
                IsPrimaryCareInCommunity: true,
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
            var neutralEnvironment = new PopulationResidentEnvironmentalHealthContextV1(
                WaterCoverageIndex: 1d,
                SanitationCoverageIndex: 1d,
                FloodingIndex: 0d,
                UtilityContinuityIndex: 1d,
                EmergencyWaterShortageRiskIndex: 0d,
                FoodShortageRiskIndex: 0d,
                MedicineShortageRiskIndex: 0d,
                EmergencyRationingEnabled: false);

            return new PopulationResidentHealthRiskBatchV2(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 23,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "health-risk:23",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentHealthRiskV2(
                        residentId, 50, 45, 60, 40, false, 80, true, "Housed", true,
                        2, 0.1d, false, neutralHousehold, neutralAccess, neutralEnvironment)
                ]);
        }
    }
}
