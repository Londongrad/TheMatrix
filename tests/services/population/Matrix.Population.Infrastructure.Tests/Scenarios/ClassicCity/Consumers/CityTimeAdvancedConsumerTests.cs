using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_WhenPhaseDoesNotMatchPopulationReaction_DoesNothing()
        {
            var mediator = new TestMediator
            {
                Result = new AdvanceCityPopulationResult(
                    Status: AdvanceCityPopulationStatus.Applied,
                    AffectedPeopleCount: 5)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateMessage(ClassicCityTickPhaseKeys.Projection),
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.Commands);
            Assert.Empty(logger.Entries);
        }

        [Fact]
        public async Task ConsumeAsync_WhenRuntimeDoesNotMatch_DoesNothing()
        {
            var mediator = new TestMediator
            {
                Result = new AdvanceCityPopulationResult(
                    AdvanceCityPopulationStatus.Applied,
                    5)
            };
            var consumer = new CityTimeAdvancedConsumer(
                mediator,
                new TestLogger<CityTimeAdvancedConsumer>());

            await consumer.ConsumeAsync(
                CreateMessage(
                    ClassicCityTickPhaseKeys.PopulationReaction,
                    scenarioKey: "metro",
                    hostTypeKey: "network"),
                CancellationToken.None);

            Assert.Empty(mediator.Commands);
        }

        [Fact]
        public async Task ConsumeAsync_WhenAdvanceIsApplied_SendsCommandAndLogsInformation()
        {
            var mediator = new TestMediator
            {
                Result = new AdvanceCityPopulationResult(
                    Status: AdvanceCityPopulationStatus.Applied,
                    AffectedPeopleCount: 7)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                logger: logger);
            SimulationTickPhaseReachedV1 message =
                CreateMessage(ClassicCityTickPhaseKeys.PopulationReaction);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            AdvanceCityPopulationCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.HostId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.TickId,
                actual: command.TickId);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied city population progression",
                actualString: entry.Message);
        }

        [Fact]
        public async Task ConsumeAsync_WhenAdvanceIsCityArchived_LogsDebug()
        {
            var mediator = new TestMediator
            {
                Result = new AdvanceCityPopulationResult(
                    Status: AdvanceCityPopulationStatus.CityArchived,
                    AffectedPeopleCount: 0)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateMessage(ClassicCityTickPhaseKeys.PopulationReaction),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "archived",
                actualString: entry.Message);
        }

        private static SimulationTickPhaseReachedV1 CreateMessage(
            string phaseKey,
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey)
        {
            return new SimulationTickPhaseReachedV1(
                SimulationId: Guid.Parse("ec4cc0b5-363a-41ec-b088-2432daa26ffd"),
                HostId: Guid.Parse("825bc802-3315-4387-8ee9-234270789660"),
                ScenarioKey: scenarioKey,
                HostTypeKey: hostTypeKey,
                PhaseKey: phaseKey,
                FromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 7,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                TickId: 42,
                SpeedMultiplier: 60m,
                ModelVersion: 1,
                CausationId: "tick:42",
                CorrelationId: "corr:42",
                OccurredOnUtc: new DateTime(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 8,
                    minute: 1,
                    second: 0,
                    kind: DateTimeKind.Utc));
        }

        private sealed class TestMediator : IMediator
        {
            public List<AdvanceCityPopulationCommand> Commands { get; } = [];
            public required AdvanceCityPopulationResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                AdvanceCityPopulationCommand command = Assert.IsType<AdvanceCityPopulationCommand>(request);
                Commands.Add(command);
                return Task.FromResult((TResponse)(object)Result);
            }

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                throw new NotSupportedException();
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                throw new NotSupportedException();
            }
        }
    }
}
