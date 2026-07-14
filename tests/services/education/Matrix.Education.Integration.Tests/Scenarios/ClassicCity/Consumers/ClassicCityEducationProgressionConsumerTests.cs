using Matrix.Education.Integration.Scenarios.ClassicCity.Consumers;
using Matrix.Education.Integration.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Education.Integration.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEducationProgressionConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_ProjectionTick_SendsProgressionCommand()
        {
            var mediator = new EducationIntegrationMediatorStub();
            var consumer = new ClassicCityEducationProgressionConsumer(
                mediator,
                NullLogger<ClassicCityEducationProgressionConsumer>.Instance);
            SimulationTickPhaseReachedV1 message = CreateMessage();

            await consumer.ConsumeAsync(message, CancellationToken.None);

            var command = Assert.Single(mediator.ProgressionCommands);
            Assert.Equal(message.HostId, command.SimulationHostId);
            Assert.Equal(message.TickId, command.TickId);
            Assert.Equal(message.ScenarioKey, command.ScenarioKey);
            Assert.Equal(message.HostTypeKey, command.HostTypeKey);
        }

        [Theory]
        [InlineData("metro-2033", "station-network", "projection")]
        [InlineData("classic-city", "city", "population-reaction")]
        public async Task ConsumeAsync_NonMatchingTick_DoesNotSendCommand(
            string scenarioKey,
            string hostTypeKey,
            string phaseKey)
        {
            var mediator = new EducationIntegrationMediatorStub();
            var consumer = new ClassicCityEducationProgressionConsumer(
                mediator,
                NullLogger<ClassicCityEducationProgressionConsumer>.Instance);

            await consumer.ConsumeAsync(
                CreateMessage(scenarioKey, hostTypeKey, phaseKey),
                CancellationToken.None);

            Assert.Empty(mediator.ProgressionCommands);
        }

        private static SimulationTickPhaseReachedV1 CreateMessage(
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey,
            string phaseKey = ClassicCityTickPhaseKeys.Projection)
        {
            return new SimulationTickPhaseReachedV1(
                SimulationId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                HostId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ScenarioKey: scenarioKey,
                HostTypeKey: hostTypeKey,
                PhaseKey: phaseKey,
                FromSimTimeUtc: new DateTimeOffset(2048, 6, 1, 0, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 6, 1, 0, 1, 0, TimeSpan.Zero),
                TickId: 42,
                SpeedMultiplier: 1m,
                ModelVersion: 1,
                CausationId: "tick:42:projection",
                CorrelationId: "tick:42",
                OccurredOnUtc: new DateTime(2048, 6, 1, 0, 0, 1, DateTimeKind.Utc));
        }
    }
}
