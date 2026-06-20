using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Events;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SyncCityOperationalBudgetPressure;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.SyncCityResourceSupply;
using Matrix.SimulationSystems.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.SimulationSystems.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.SimulationSystems.Infrastructure.Tests.TestSupport.SimulationSystemsInfrastructureTestSupport;

namespace Matrix.SimulationSystems.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityEnvironmentalSyncConsumersTests
    {
        [Fact]
        public async Task CityWeatherCreatedConsumeAsync_WhenApplied_SendsMappedCommandAndLogsInformation()
        {
            CityWeatherCreatedV1 message = CreateWeatherCreatedMessage();
            var mediator = new TestRecalculateMediator
            {
                Result = new RecalculateCityEnvironmentalConditionsResult(
                    Status: RecalculateCityEnvironmentalConditionsStatus.Applied,
                    FloodingIndex: 0.18m,
                    SnowAccumulationIndex: 0.24m,
                    RoadAccessibilityIndex: 0.71m)
            };
            var logger = new TestLogger<CityWeatherCreatedConsumer>();
            var consumer = new CityWeatherCreatedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            RecalculateCityEnvironmentalConditionsCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.CityId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.AtSimTimeUtc,
                actual: command.AtSimTimeUtc);
            Assert.Equal(
                expected: message.InitialState.Type,
                actual: command.Weather.Type);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "weather-initialization",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityWeatherChangedConsumeAsync_WhenStale_LogsWarning()
        {
            var mediator = new TestRecalculateMediator
            {
                Result = new RecalculateCityEnvironmentalConditionsResult(
                    Status: RecalculateCityEnvironmentalConditionsStatus.Stale,
                    FloodingIndex: 0m,
                    SnowAccumulationIndex: 0m,
                    RoadAccessibilityIndex: 0m)
            };
            var logger = new TestLogger<CityWeatherChangedConsumer>();
            var consumer = new CityWeatherChangedConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateWeatherChangedMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Warning,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped stale classic city environmental weather sync",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityOperationalBudgetPressureConsumeAsync_WhenApplied_SendsCommandAndLogsInformation()
        {
            ClassicCityOperationalBudgetPressureSnapshotV1 message = CreateBudgetPressureMessage();
            var mediator = new TestBudgetPressureMediator
            {
                Result = new SyncCityOperationalBudgetPressureResult(
                    Status: SyncCityOperationalBudgetPressureStatus.Applied,
                    PressureIndex: message.PressureIndex,
                    EffectiveTickId: message.EffectiveTickId,
                    EffectiveAtUtc: message.EffectiveAtUtc)
            };
            var logger = new TestLogger<CityOperationalBudgetPressureConsumer>();
            var consumer = new CityOperationalBudgetPressureConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            SyncCityOperationalBudgetPressureCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.CityId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.PressureIndex,
                actual: command.PressureIndex);
            Assert.Equal(
                expected: message.EffectiveTickId,
                actual: command.EffectiveTickId);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied classic city operational budget pressure",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityStockpileSnapshotConsumeAsync_WhenDeferred_LogsDebug()
        {
            var mediator = new TestStockpileMediator
            {
                Result = new SyncCityResourceSupplyResult(
                    Status: SyncCityResourceSupplyStatus.Deferred,
                    SupplyStressIndex: 0.42m,
                    EffectiveTickId: 12,
                    EffectiveAtUtc: LaterUtc)
            };
            var logger = new TestLogger<CityStockpileSnapshotConsumer>();
            var consumer = new CityStockpileSnapshotConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateStockpileSnapshotMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Deferred classic city resource supply snapshot",
                actualString: entry.Message);
        }

        private static CityWeatherCreatedV1 CreateWeatherCreatedMessage()
        {
            return new CityWeatherCreatedV1(
                CityId: CityId,
                ClimateProfile: new WeatherClimateProfileV1(
                    ClimateZone: "Temperate",
                    Volatility: 0.3m,
                    MaxOverallSeverity: "Severe",
                    SupportsThunderstorms: true,
                    SupportsSnowstorms: true,
                    SupportsFog: true,
                    SupportsHeatwaves: true),
                InitialState: CreateWeatherState(
                    type: "Snowstorm",
                    severity: "Severe"),
                AtSimTimeUtc: LaterUtc,
                OccurredOnUtc: LaterUtc.UtcDateTime);
        }

        private static CityWeatherChangedV1 CreateWeatherChangedMessage()
        {
            return new CityWeatherChangedV1(
                CityId: CityId,
                PreviousState: CreateWeatherState(
                    type: "Clear",
                    severity: "Calm"),
                CurrentState: CreateWeatherState(
                    type: "Storm",
                    severity: "Severe"),
                AtSimTimeUtc: LaterUtc,
                OccurredOnUtc: LaterUtc.UtcDateTime);
        }

        private static WeatherStateV1 CreateWeatherState(
            string type,
            string severity)
        {
            return new WeatherStateV1(
                Type: type,
                Severity: severity,
                PrecipitationKind: "Rain",
                TemperatureC: 4m,
                HumidityPercent: 81m,
                WindSpeedKph: 36m,
                CloudCoveragePercent: 88m,
                PressureHpa: 995m,
                StartedAtUtc: CreatedAtUtc,
                ExpectedUntilUtc: LaterUtc.AddHours(2));
        }

        private static ClassicCityOperationalBudgetPressureSnapshotV1 CreateBudgetPressureMessage()
        {
            return new ClassicCityOperationalBudgetPressureSnapshotV1(
                CityId: CityId,
                Balance: 4200m,
                TotalCityExpenses: 390m,
                MunicipalOperationsExpenses: 140m,
                InfrastructureOperationsExpenses: 100m,
                EmergencyOperationsExpenses: 35m,
                GeneralAvailableAmount: 900m,
                OperationsAvailableAmount: 720m,
                InfrastructureAvailableAmount: 610m,
                HealthcareAvailableAmount: 560m,
                GeneralAuthorizationLevel: "Open",
                OperationsAuthorizationLevel: "Guarded",
                InfrastructureAuthorizationLevel: "Guarded",
                HealthcareAuthorizationLevel: "Protected",
                PressureIndex: 0.38m,
                EffectiveTickId: 11,
                EffectiveAtUtc: LaterUtc,
                OccurredAtUtc: LaterUtc.AddMinutes(1));
        }

        private static ClassicCityStockpileSnapshotV1 CreateStockpileSnapshotMessage()
        {
            var line = new ClassicCityStockpileLineSnapshotV1(
                Kind: "Fuel",
                StockLevelIndex: 0.63m,
                DemandPressureIndex: 0.22m,
                ResupplyReadinessIndex: 0.74m,
                ShortageRiskIndex: 0.19m);

            return new ClassicCityStockpileSnapshotV1(
                CityId: CityId,
                SupplyStressIndex: 0.28m,
                EmergencyRationingEnabled: false,
                Fuel: line,
                Food: line with
                {
                    Kind = "Food"
                },
                Medicine: line with
                {
                    Kind = "Medicine"
                },
                SpareParts: line with
                {
                    Kind = "SpareParts"
                },
                Filters: line with
                {
                    Kind = "Filters"
                },
                EmergencyWater: line with
                {
                    Kind = "EmergencyWater"
                },
                EffectiveTickId: 12,
                EffectiveAtUtc: LaterUtc,
                OccurredAtUtc: LaterUtc.AddMinutes(2));
        }

        private sealed class TestRecalculateMediator : IMediator
        {
            public List<RecalculateCityEnvironmentalConditionsCommand> Commands { get; } = [];
            public required RecalculateCityEnvironmentalConditionsResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                RecalculateCityEnvironmentalConditionsCommand command =
                    Assert.IsType<RecalculateCityEnvironmentalConditionsCommand>(request);
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

        private sealed class TestBudgetPressureMediator : IMediator
        {
            public List<SyncCityOperationalBudgetPressureCommand> Commands { get; } = [];
            public required SyncCityOperationalBudgetPressureResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                SyncCityOperationalBudgetPressureCommand command =
                    Assert.IsType<SyncCityOperationalBudgetPressureCommand>(request);
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

        private sealed class TestStockpileMediator : IMediator
        {
            public List<SyncCityResourceSupplyCommand> Commands { get; } = [];
            public required SyncCityResourceSupplyResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                SyncCityResourceSupplyCommand command = Assert.IsType<SyncCityResourceSupplyCommand>(request);
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
