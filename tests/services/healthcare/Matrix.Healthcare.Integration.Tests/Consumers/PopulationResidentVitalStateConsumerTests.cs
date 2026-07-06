using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.Population.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentVitalStateConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsPatientInitializationCommand()
        {
            var mediator = new HealthcareIntegrationMediatorStub
            {
                MedicalResult = new InitializePatientMedicalRecordsResult(
                    InitializePatientMedicalRecordsStatus.Applied,
                    AddedRecords: 1,
                    IgnoredRecords: 0)
            };
            var consumer = new PopulationResidentVitalStateConsumer(
                mediator,
                NullLogger<PopulationResidentVitalStateConsumer>.Instance);
            Guid residentId = Guid.NewGuid();
            var message = new PopulationResidentVitalStateBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 3,
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "vital-state:3",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentVitalStateV1(
                        ResidentId: residentId,
                        HealthScore: 70,
                        LifecycleRevision: 2)
                ]);

            await consumer.ConsumeAsync(message, CancellationToken.None);

            InitializePatientMedicalRecordsCommand command = Assert.Single(mediator.MedicalCommands);
            InitializePatientMedicalRecordItem record = Assert.Single(command.Records);
            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            Assert.Equal(message.SourceRevision, command.SourceRevision);
            Assert.Equal(residentId, record.PatientId);
            Assert.Equal(70, record.HealthScore);
            Assert.Equal(2, record.LifecycleRevision);
        }

        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                "healthcare-population-resident-vital-state-v1",
                PopulationResidentVitalStateConsumerDefinition.EndpointNameValue);
            Assert.Equal(4, PopulationResidentVitalStateConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
