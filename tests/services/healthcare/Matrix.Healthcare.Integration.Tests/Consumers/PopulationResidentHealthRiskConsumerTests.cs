using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.Population.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentHealthRiskConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsPatientHealthProgressionCommand()
        {
            var mediator = new HealthcareIntegrationMediatorStub();
            var consumer = new PopulationResidentHealthRiskConsumer(
                mediator,
                NullLogger<PopulationResidentHealthRiskConsumer>.Instance);
            Guid residentId = Guid.NewGuid();
            var message = new PopulationResidentHealthRiskBatchV1(
                SimulationHostId: Guid.NewGuid(),
                SourceRevision: 22,
                PreviousDate: new DateOnly(2048, 5, 5),
                CurrentDate: new DateOnly(2048, 5, 6),
                ObservedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "health-risk:22",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentHealthRiskV1(
                        residentId, 50, 45, 60, 40, false, "Housed", true,
                        2, 0.1d, false, 0.2d, 0.3d)
                ]);

            await consumer.ConsumeAsync(message, CancellationToken.None);

            AdvancePatientHealthCommand command = Assert.Single(mediator.HealthProgressionCommands);
            Assert.Equal(22, command.SourceRevision);
            Assert.Equal(residentId, Assert.Single(command.Patients).PatientId);
        }

        [Fact]
        public void EndpointConstants_AreStableAndBoundConcurrency()
        {
            Assert.Equal(
                "healthcare-population-resident-health-risk-v1",
                PopulationResidentHealthRiskConsumerDefinition.EndpointNameValue);
            Assert.Equal(4, PopulationResidentHealthRiskConsumerDefinition.ConcurrentMessageLimitValue);
        }
    }
}
