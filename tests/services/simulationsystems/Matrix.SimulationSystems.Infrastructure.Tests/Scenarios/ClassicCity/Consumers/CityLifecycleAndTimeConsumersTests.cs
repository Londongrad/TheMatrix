using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityLifecycleAndTimeConsumersTests
    {
        [Fact]
        public async Task CityCreatedConsumeAsync_WhenRuntimeKeysDoNotMatch_DoesNothingAndLogsDebug()
        {
            var mediator = new TestSeedMediator
            {
                Result = new SeedCityEnvironmentalConditionsResult(
                    Status: SeedCityEnvironmentalConditionsStatus.Applied,
                    LastEvaluatedAtUtc: CreatedAtUtc)
            };
            var logger = new TestLogger<CityCreatedConsumer>();
            var consumer = new CityCreatedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateCityCreatedMessage(
                    scenarioKey: "metro",
                    hostTypeKey: "network"),
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.Commands);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Ignored classic-city-created event",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityCreatedConsumeAsync_WhenSeedIsApplied_SendsCommandAndLogsInformation()
        {
            ClassicCityCreatedV1 message = CreateCityCreatedMessage();
            var mediator = new TestSeedMediator
            {
                Result = new SeedCityEnvironmentalConditionsResult(
                    Status: SeedCityEnvironmentalConditionsStatus.Applied,
                    LastEvaluatedAtUtc: LaterUtc)
            };
            var logger = new TestLogger<CityCreatedConsumer>();
            var consumer = new CityCreatedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            SeedCityEnvironmentalConditionsCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.HostId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.CreatedAtUtc,
                actual: command.CreatedAtUtc);
            Assert.Equal(
                expected: message.DevelopmentLevel,
                actual: command.DevelopmentLevel);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Initialized classic city environmental state",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityTimeAdvancedConsumeAsync_WhenPhaseDoesNotMatch_DoesNothing()
        {
            var mediator = new TestAdvanceMediator
            {
                Result = new AdvanceCityEnvironmentalConditionsResult(
                    Status: AdvanceCityEnvironmentalConditionsStatus.Applied,
                    ProcessedSimMinutes: 60,
                    FloodingIndex: 0.12m,
                    SnowAccumulationIndex: 0.31m,
                    RoadAccessibilityIndex: 0.77m)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateTickMessage(ClassicCityTickPhaseKeys.Projection),
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.Commands);
            Assert.Empty(logger.Entries);
        }

        [Fact]
        public async Task CityTimeAdvancedConsumeAsync_WhenRuntimeDoesNotMatch_DoesNothing()
        {
            var mediator = new TestAdvanceMediator
            {
                Result = new AdvanceCityEnvironmentalConditionsResult(
                    Status: AdvanceCityEnvironmentalConditionsStatus.Applied,
                    ProcessedSimMinutes: 60,
                    FloodingIndex: 0.12m,
                    SnowAccumulationIndex: 0.31m,
                    RoadAccessibilityIndex: 0.77m)
            };
            var consumer = new CityTimeAdvancedConsumer(
                mediator,
                new TestLogger<CityTimeAdvancedConsumer>());

            await consumer.ConsumeAsync(
                CreateTickMessage(
                    ClassicCityTickPhaseKeys.SystemsDegradation,
                    scenarioKey: "metro",
                    hostTypeKey: "network"),
                CancellationToken.None);

            Assert.Empty(mediator.Commands);
        }

        [Fact]
        public async Task CityTimeAdvancedConsumeAsync_WhenAdvanceIsApplied_SendsCommandAndLogsInformation()
        {
            SimulationTickPhaseReachedV1 message =
                CreateTickMessage(ClassicCityTickPhaseKeys.SystemsDegradation);
            var mediator = new TestAdvanceMediator
            {
                Result = new AdvanceCityEnvironmentalConditionsResult(
                    Status: AdvanceCityEnvironmentalConditionsStatus.Applied,
                    ProcessedSimMinutes: 120,
                    FloodingIndex: 0.27m,
                    SnowAccumulationIndex: 0.35m,
                    RoadAccessibilityIndex: 0.69m)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            AdvanceCityEnvironmentalConditionsCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.HostId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.FromSimTimeUtc,
                actual: command.FromSimTimeUtc);
            Assert.Equal(
                expected: message.ToSimTimeUtc,
                actual: command.ToSimTimeUtc);
            Assert.Equal(
                expected: message.TickId,
                actual: command.TickId);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied classic city environmental time progression",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityTimeAdvancedConsumeAsync_WhenAdvanceIsDuplicate_LogsDebug()
        {
            var mediator = new TestAdvanceMediator
            {
                Result = new AdvanceCityEnvironmentalConditionsResult(
                    Status: AdvanceCityEnvironmentalConditionsStatus.Duplicate,
                    ProcessedSimMinutes: 0,
                    FloodingIndex: 0m,
                    SnowAccumulationIndex: 0m,
                    RoadAccessibilityIndex: 0m)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateTickMessage(ClassicCityTickPhaseKeys.SystemsDegradation),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped duplicate classic city environmental time progression",
                actualString: entry.Message);
        }

        private static ClassicCityCreatedV1 CreateCityCreatedMessage(
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey)
        {
            return new ClassicCityCreatedV1(
                SimulationId: Guid.Parse("94c5dc18-f29b-4055-8b79-fcd49ca62b76"),
                HostId: CityId,
                ScenarioKey: scenarioKey,
                HostTypeKey: hostTypeKey,
                Name: "Northreach",
                CreatedAtUtc: CreatedAtUtc,
                DevelopmentLevel: "advanced",
                EconomyProfile: "balanced",
                RunId: Guid.Parse("df05950d-dedf-490c-93e8-c2579026bab8"),
                SimulationSeed: "systems-seed",
                ScenarioModelSetVersion: "classic-city-v3");
        }

        private static SimulationTickPhaseReachedV1 CreateTickMessage(
            string phaseKey,
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey)
        {
            return new SimulationTickPhaseReachedV1(
                SimulationId: Guid.Parse("94c5dc18-f29b-4055-8b79-fcd49ca62b76"),
                HostId: CityId,
                ScenarioKey: scenarioKey,
                HostTypeKey: hostTypeKey,
                PhaseKey: phaseKey,
                FromSimTimeUtc: CreatedAtUtc,
                ToSimTimeUtc: LaterUtc,
                TickId: 9,
                SpeedMultiplier: 60m,
                ModelVersion: 1,
                CausationId: "tick:9",
                CorrelationId: "corr:9",
                OccurredOnUtc: LaterUtc.UtcDateTime);
        }

        private sealed class TestSeedMediator : IMediator
        {
            public List<SeedCityEnvironmentalConditionsCommand> Commands { get; } = [];
            public required SeedCityEnvironmentalConditionsResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                SeedCityEnvironmentalConditionsCommand command =
                    Assert.IsType<SeedCityEnvironmentalConditionsCommand>(request);
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

        private sealed class TestAdvanceMediator : IMediator
        {
            public List<AdvanceCityEnvironmentalConditionsCommand> Commands { get; } = [];
            public required AdvanceCityEnvironmentalConditionsResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                AdvanceCityEnvironmentalConditionsCommand command =
                    Assert.IsType<AdvanceCityEnvironmentalConditionsCommand>(request);
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
