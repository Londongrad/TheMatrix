using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class HealthcarePatientHealthOutcomeCommandMapperTests
    {
        [Fact]
        public void Map_ValidOutcome_PreservesEnvelopeAndMedicalSnapshot()
        {
            Guid messageId = Guid.NewGuid();
            Guid patientId = Guid.NewGuid();
            HealthcarePatientHealthOutcomeBatchV1 message = CreateMessage(
                new HealthcarePatientHealthOutcomeV1(
                    PatientId: patientId,
                    HealthScore: 64,
                    CurrentIllnessKind: "Infection",
                    CurrentIllnessSeverity: "Moderate",
                    DiagnosedOn: new DateOnly(2048, 5, 4),
                    LastRecoveredOn: null,
                    HealthDelta: -2,
                    HappinessDelta: -2,
                    EnergyDelta: -2,
                    StressDelta: 2,
                    BecameCritical: false,
                    LifecycleRevision: 3,
                    FunctionalCapacityScore: 60));

            ApplyPatientHealthOutcomesCommand command =
                HealthcarePatientHealthOutcomeCommandMapper.Map(
                    message,
                    messageId,
                    HealthcarePatientHealthOutcomeConsumerDefinition.EndpointNameValue);

            Assert.Equal(message.SimulationHostId, command.CityId);
            Assert.Equal(messageId, command.IntegrationMessageId);
            Assert.Equal(17, command.SourceRevision);
            Assert.Equal(new DateOnly(2048, 5, 6), command.CurrentDate);
            PatientHealthOutcomeInput patient = Assert.Single(command.Patients);
            Assert.Equal(patientId, patient.PatientId);
            Assert.Equal(IllnessKind.Infection, patient.CurrentIllnessKind);
            Assert.Equal(IllnessSeverity.Moderate, patient.CurrentIllnessSeverity);
            Assert.Equal(60, patient.FunctionalCapacityScore);
            Assert.Equal(3, patient.LifecycleRevision);
        }

        [Fact]
        public void Map_MismatchedIllnessSnapshot_ThrowsArgumentException()
        {
            HealthcarePatientHealthOutcomeBatchV1 message = CreateMessage(
                new HealthcarePatientHealthOutcomeV1(
                    PatientId: Guid.NewGuid(),
                    HealthScore: 64,
                    CurrentIllnessKind: "Infection",
                    CurrentIllnessSeverity: null,
                    DiagnosedOn: new DateOnly(2048, 5, 4),
                    LastRecoveredOn: null,
                    HealthDelta: -2,
                    HappinessDelta: -2,
                    EnergyDelta: -2,
                    StressDelta: 2,
                    BecameCritical: false));

            Assert.Throws<ArgumentException>(() =>
                HealthcarePatientHealthOutcomeCommandMapper.Map(
                    message,
                    Guid.NewGuid(),
                    HealthcarePatientHealthOutcomeConsumerDefinition.EndpointNameValue));
        }

        [Fact]
        public void ConsumerDefinition_SerializesOutcomeApplication()
        {
            Assert.Equal(
                "population-healthcare-patient-health-outcome-v1",
                HealthcarePatientHealthOutcomeConsumerDefinition.EndpointNameValue);
            Assert.Equal(1, HealthcarePatientHealthOutcomeConsumerDefinition.ConcurrentMessageLimitValue);
        }

        private static HealthcarePatientHealthOutcomeBatchV1 CreateMessage(
            params HealthcarePatientHealthOutcomeV1[] patients)
        {
            return new HealthcarePatientHealthOutcomeBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 17,
                CurrentDate: new DateOnly(2048, 5, 6),
                OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
                CorrelationId: "healthcare:city:17:outcome",
                BatchNumber: 1,
                TotalBatches: 1,
                Patients: patients);
        }
    }
}
