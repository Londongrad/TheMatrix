using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.Population.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentMedicalStateConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsMedicalRecordInitializationCommand()
        {
            var mediator = new HealthcareIntegrationMediatorStub
            {
                MedicalResult = new InitializePatientMedicalRecordsResult(
                    InitializePatientMedicalRecordsStatus.Applied,
                    AddedRecords: 1,
                    IgnoredRecords: 0)
            };
            var consumer = new PopulationResidentMedicalStateConsumer(
                mediator,
                NullLogger<PopulationResidentMedicalStateConsumer>.Instance);
            Guid residentId = Guid.NewGuid();
            var message = new PopulationResidentMedicalStateBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 3,
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "medical-state:3",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentMedicalStateV1(
                        ResidentId: residentId,
                        HealthScore: 70,
                        CurrentIllnessKind: "Stress",
                        CurrentIllnessSeverity: "Mild",
                        DiagnosedOn: new DateOnly(2048, 5, 4),
                        LastRecoveredOn: null,
                        LifecycleRevision: 2)
                ]);

            await consumer.ConsumeAsync(message, CancellationToken.None);

            InitializePatientMedicalRecordsCommand command = Assert.Single(mediator.MedicalCommands);
            InitializePatientMedicalRecordItem record = Assert.Single(command.Records);
            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            Assert.Equal(message.SourceRevision, command.SourceRevision);
            Assert.Equal(residentId, record.PatientId);
            Assert.Equal(IllnessKind.Stress, record.CurrentIllnessKind);
            Assert.Equal(2, record.LifecycleRevision);
        }

        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                "healthcare-population-resident-medical-state-v1",
                PopulationResidentMedicalStateConsumerDefinition.EndpointNameValue);
            Assert.Equal(4, PopulationResidentMedicalStateConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
