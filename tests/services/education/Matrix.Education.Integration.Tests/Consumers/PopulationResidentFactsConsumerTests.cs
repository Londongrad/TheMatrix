using Matrix.Education.Application.Students.SynchronizeStudentProfiles;
using Matrix.Education.Integration.Consumers;
using Matrix.Education.Integration.Tests.TestSupport;
using Matrix.Population.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers
{
    public sealed class PopulationResidentFactsConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsMappedSynchronizationCommand()
        {
            var mediator = new EducationIntegrationMediatorStub
            {
                ProfileResult = new SynchronizeStudentProfilesResult(
                    Status: SynchronizeStudentProfilesStatus.Applied,
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
                        IsActive: true,
                        LifecycleRevision: 6)
                ]);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            SynchronizeStudentProfilesCommand command = Assert.Single(mediator.ProfileCommands);
            Assert.Equal(
                expected: message.SimulationHostId,
                actual: command.SimulationHostId);
            SynchronizeStudentProfileItem profile = Assert.Single(command.Profiles);
            Assert.Equal(
                expected: message.Residents[0].ResidentId,
                actual: profile.ResidentId);
            Assert.Equal(
                expected: message.SourceRevision,
                actual: profile.SourceRevision);
            Assert.Equal(
                expected: 6,
                actual: profile.LifecycleRevision);
        }

    }
}
