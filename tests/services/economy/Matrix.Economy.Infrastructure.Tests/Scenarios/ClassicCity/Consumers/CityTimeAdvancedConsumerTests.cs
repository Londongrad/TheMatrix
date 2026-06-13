using Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityTimeAdvancedConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_WhenPhaseDoesNotMatchBudgetSettlement_DoesNothing()
        {
            var mediator = new TestMediator
            {
                Result = new AdvanceCityEconomySimulationResult(
                    Status: AdvanceCityEconomySimulationStatus.Applied,
                    ProcessedDays: 1,
                    ChargedObligations: 1,
                    RemittedBusinesses: 1,
                    MunicipalProviderPayments: 1,
                    TotalChargedAmount: 10m,
                    TotalTaxRemittedAmount: 5m,
                    TotalMunicipalDisbursedAmount: 2m)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                deletionRepository: new TestCityEconomyDeletionRepository(),
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
                Result = new AdvanceCityEconomySimulationResult(
                    AdvanceCityEconomySimulationStatus.Applied,
                    1,
                    1,
                    1,
                    1,
                    10m,
                    5m,
                    2m)
            };
            var consumer = new CityTimeAdvancedConsumer(
                mediator,
                new TestCityEconomyDeletionRepository(),
                new TestLogger<CityTimeAdvancedConsumer>());

            await consumer.ConsumeAsync(
                CreateMessage(
                    ClassicCityTickPhaseKeys.BudgetSettlement,
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
                Result = new AdvanceCityEconomySimulationResult(
                    Status: AdvanceCityEconomySimulationStatus.Applied,
                    ProcessedDays: 2,
                    ChargedObligations: 3,
                    RemittedBusinesses: 4,
                    MunicipalProviderPayments: 5,
                    TotalChargedAmount: 10m,
                    TotalTaxRemittedAmount: 6m,
                    TotalMunicipalDisbursedAmount: 7m)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                logger: logger);
            SimulationTickPhaseReachedV1 message =
                CreateMessage(ClassicCityTickPhaseKeys.BudgetSettlement);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            AdvanceCityEconomySimulationCommand command = Assert.Single(mediator.Commands);
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
                expectedSubstring: "Applied city economy progression",
                actualString: entry.Message);
        }

        [Fact]
        public async Task ConsumeAsync_WhenAdvanceIsDuplicate_LogsDebug()
        {
            var mediator = new TestMediator
            {
                Result = new AdvanceCityEconomySimulationResult(
                    Status: AdvanceCityEconomySimulationStatus.Duplicate,
                    ProcessedDays: 0,
                    ChargedObligations: 0,
                    RemittedBusinesses: 0,
                    MunicipalProviderPayments: 0,
                    TotalChargedAmount: 0m,
                    TotalTaxRemittedAmount: 0m,
                    TotalMunicipalDisbursedAmount: 0m)
            };
            var logger = new TestLogger<CityTimeAdvancedConsumer>();
            var consumer = new CityTimeAdvancedConsumer(
                mediator: mediator,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateMessage(ClassicCityTickPhaseKeys.BudgetSettlement),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped duplicate city economy progression",
                actualString: entry.Message);
        }

        private static SimulationTickPhaseReachedV1 CreateMessage(
            string phaseKey,
            string scenarioKey = ClassicCityRuntimeKeys.ScenarioKey,
            string hostTypeKey = ClassicCityRuntimeKeys.HostTypeKey)
        {
            return new SimulationTickPhaseReachedV1(
                SimulationId: Guid.Parse("176c7256-66f0-4a7f-9378-260ddf3d9940"),
                HostId: Guid.Parse("aa1b729e-b694-4f81-aaf0-88db3757b4af"),
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
            public List<AdvanceCityEconomySimulationCommand> Commands { get; } = [];

            public required AdvanceCityEconomySimulationResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                AdvanceCityEconomySimulationCommand command =
                    Assert.IsType<AdvanceCityEconomySimulationCommand>(request);
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
