using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Integration.Consumers;
using Matrix.Healthcare.Integration.Tests.TestSupport;
using Matrix.Population.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsMappedPatientSynchronizationCommand()
        {
            var mediator = new HealthcareIntegrationMediatorStub
            {
                Result = new SynchronizePatientProfilesResult(
                    Status: SynchronizePatientProfilesStatus.Applied,
                    AddedProfiles: 1,
                    UpdatedProfiles: 0,
                    IgnoredProfiles: 0)
            };
            var consumer = new PopulationResidentFactsConsumer(
                mediator: mediator,
                logger: NullLogger<PopulationResidentFactsConsumer>.Instance);
            var message = new PopulationResidentFactsBatchV1(
                SimulationHostId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                SourceRevision: 14,
                SynchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"),
                CorrelationId: "resident-facts:14",
                BatchNumber: 1,
                TotalBatches: 1,
                Residents:
                [
                    new PopulationResidentFactsV1(
                        ResidentId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                        BirthDate: new DateOnly(2027, 4, 3),
                        Sex: "Female",
                        IsAlive: true,
                        IsActive: true)
                ]);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            SynchronizePatientProfilesCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(message.SimulationHostId, command.SimulationHostId);
            SynchronizePatientProfileItem profile = Assert.Single(command.Profiles);
            Assert.Equal(message.Residents[0].ResidentId, profile.PatientId);
            Assert.Equal(PatientSex.Female, profile.Sex);
            Assert.Equal(message.SourceRevision, profile.SourceRevision);
        }
    }
}
