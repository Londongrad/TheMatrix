using Matrix.Education.Integration.Consumers;
using Matrix.Education.Integration.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Education.Integration.Tests.Consumers
{
    public sealed class SimulationDeletedConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_SendsDeletionForSimulationHost()
        {
            var mediator = new EducationIntegrationMediatorStub();
            var consumer = new SimulationDeletedConsumer(
                mediator: mediator,
                logger: NullLogger<SimulationDeletedConsumer>.Instance);
            var message = new SimulationDeletedV1(
                SimulationId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                HostId: Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"),
                ScenarioKey: "metro",
                HostTypeKey: "network",
                DeletedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"));

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            var command = Assert.Single(mediator.DeletionCommands);
            Assert.Equal(
                expected: message.HostId,
                actual: command.SimulationHostId);
            Assert.Equal(
                expected: message.DeletedAtUtc,
                actual: command.DeletedAtUtc);
        }
    }
}
