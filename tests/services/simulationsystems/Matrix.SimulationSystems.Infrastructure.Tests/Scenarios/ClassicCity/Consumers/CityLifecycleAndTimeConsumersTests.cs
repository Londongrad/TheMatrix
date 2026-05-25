using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity;
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
        public async Task CityCreatedConsumeAsync_WhenSimulationKindDoesNotMatch_DoesNothingAndLogsDebug()
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
                message: CreateCityCreatedMessage("Sandbox"),
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.Commands);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Ignored city-created event",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityCreatedConsumeAsync_WhenSeedIsApplied_SendsCommandAndLogsInformation()
        {
            CityCreatedV1 message = CreateCityCreatedMessage(ClassicCityScenario.Name);
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
                expected: message.CityId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.CreatedAtUtc,
                actual: command.CreatedAtUtc);
            Assert.Equal(
                expected: message.SimulationKind,
                actual: command.SimulationKind);
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
                message: CreateTickMessage(CityTickPhaseV1.Projection),
                cancellationToken: CancellationToken.None);

            Assert.Empty(mediator.Commands);
            Assert.Empty(logger.Entries);
        }

        [Fact]
        public async Task CityTimeAdvancedConsumeAsync_WhenAdvanceIsApplied_SendsCommandAndLogsInformation()
        {
            CityTickPhaseReachedV1 message = CreateTickMessage(CityTickPhaseV1.SystemsDegradation);
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
                expected: message.CityId,
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
                message: CreateTickMessage(CityTickPhaseV1.SystemsDegradation),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped duplicate classic city environmental time progression",
                actualString: entry.Message);
        }

        private static CityCreatedV1 CreateCityCreatedMessage(string simulationKind)
        {
            return new CityCreatedV1(
                CityId: CityId,
                Name: "Northreach",
                SimulationKind: simulationKind,
                CreatedAtUtc: CreatedAtUtc,
                DevelopmentLevel: "advanced",
                EconomyProfile: "balanced");
        }

        private static CityTickPhaseReachedV1 CreateTickMessage(CityTickPhaseV1 phase)
        {
            return new CityTickPhaseReachedV1(
                CityId: CityId,
                FromSimTimeUtc: CreatedAtUtc,
                ToSimTimeUtc: LaterUtc,
                TickId: 9,
                SpeedMultiplier: 60m,
                TickContext: new CityTickContextV1(
                    SimulationId: Guid.Parse("94c5dc18-f29b-4055-8b79-fcd49ca62b76"),
                    CityId: CityId,
                    SimulationKind: ClassicCityScenario.Name,
                    TickId: 9,
                    EffectiveSimTimeUtc: LaterUtc,
                    Phase: phase,
                    ModelVersion: 1,
                    CausationId: "tick:9",
                    CorrelationId: "corr:9"),
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
