using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Resources;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityDemandSnapshotConsumersTests
    {
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
                expected: message.Balance,
                actual: command.Balance);
            Assert.Equal(
                expected: message.MunicipalOperationsExpenses,
                actual: command.MunicipalOperationsExpenses);
            Assert.Equal(
                expected: message.GeneralAvailableAmount,
                actual: command.GeneralAvailableAmount);
            Assert.Equal(
                expected: message.OperationsAvailableAmount,
                actual: command.OperationsAvailableAmount);
            Assert.Equal(
                expected: message.InfrastructureAvailableAmount,
                actual: command.InfrastructureAvailableAmount);
            Assert.Equal(
                expected: message.HealthcareAvailableAmount,
                actual: command.HealthcareAvailableAmount);
            Assert.Equal(
                expected: message.GeneralAuthorizationLevel,
                actual: command.GeneralAuthorizationLevel);
            Assert.Equal(
                expected: message.OperationsAuthorizationLevel,
                actual: command.OperationsAuthorizationLevel);
            Assert.Equal(
                expected: message.InfrastructureAuthorizationLevel,
                actual: command.InfrastructureAuthorizationLevel);
            Assert.Equal(
                expected: message.HealthcareAuthorizationLevel,
                actual: command.HealthcareAuthorizationLevel);
            Assert.Equal(
                expected: message.PressureIndex,
                actual: command.PressureIndex);
            Assert.Equal(
                expected: message.EffectiveTickId,
                actual: command.EffectiveTickId);
            Assert.Equal(
                expected: message.EffectiveAtUtc,
                actual: command.EffectiveAtUtc);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied classic city operational budget pressure",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityOperationalBudgetPressureConsumeAsync_WhenStale_LogsWarning()
        {
            var mediator = new TestBudgetPressureMediator
            {
                Result = new SyncCityOperationalBudgetPressureResult(
                    Status: SyncCityOperationalBudgetPressureStatus.Stale,
                    PressureIndex: 0.1m,
                    EffectiveTickId: 5,
                    EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc)
            };
            var logger = new TestLogger<CityOperationalBudgetPressureConsumer>();
            var consumer = new CityOperationalBudgetPressureConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateBudgetPressureMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Warning,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped stale classic city operational budget pressure",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CityOperationalBudgetPressureConsumeAsync_WhenStateIsNotInitialized_LogsDebug()
        {
            var mediator = new TestBudgetPressureMediator
            {
                Result = new SyncCityOperationalBudgetPressureResult(
                    Status: SyncCityOperationalBudgetPressureStatus.NotInitialized,
                    PressureIndex: 0m,
                    EffectiveTickId: 0,
                    EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc)
            };
            var logger = new TestLogger<CityOperationalBudgetPressureConsumer>();
            var consumer = new CityOperationalBudgetPressureConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateBudgetPressureMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "stockpiles are not initialized yet",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CitySystemsResourceDemandConsumeAsync_WhenApplied_SendsCommandAndLogsInformation()
        {
            ClassicCitySystemsResourceDemandSnapshotV1 message = CreateSystemsDemandMessage();
            var mediator = new TestSystemsDemandMediator
            {
                Result = new SyncCitySystemsDemandResult(
                    Status: SyncCitySystemsDemandStatus.Applied,
                    OverallDemandPressureIndex: message.OverallDemandPressureIndex,
                    EffectiveTickId: message.EffectiveTickId,
                    EffectiveAtUtc: message.EffectiveAtUtc)
            };
            var logger = new TestLogger<CitySystemsResourceDemandConsumer>();
            var consumer = new CitySystemsResourceDemandConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            SyncCitySystemsDemandCommand command = Assert.Single(mediator.Commands);
            Assert.Equal(
                expected: message.CityId,
                actual: command.CityId);
            Assert.Equal(
                expected: message.FuelDemandPressureIndex,
                actual: command.FuelDemandPressureIndex);
            Assert.Equal(
                expected: message.SparePartsDemandPressureIndex,
                actual: command.SparePartsDemandPressureIndex);
            Assert.Equal(
                expected: message.FiltersDemandPressureIndex,
                actual: command.FiltersDemandPressureIndex);
            Assert.Equal(
                expected: message.EmergencyWaterDemandPressureIndex,
                actual: command.EmergencyWaterDemandPressureIndex);
            Assert.Equal(
                expected: message.OverallDemandPressureIndex,
                actual: command.OverallDemandPressureIndex);
            Assert.Equal(
                expected: message.EffectiveTickId,
                actual: command.EffectiveTickId);
            Assert.Equal(
                expected: message.EffectiveAtUtc,
                actual: command.EffectiveAtUtc);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied classic city systems resource demand",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CitySystemsResourceDemandConsumeAsync_WhenDeferred_LogsDebug()
        {
            var mediator = new TestSystemsDemandMediator
            {
                Result = new SyncCitySystemsDemandResult(
                    Status: SyncCitySystemsDemandStatus.Deferred,
                    OverallDemandPressureIndex: 0.4m,
                    EffectiveTickId: 8,
                    EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc)
            };
            var logger = new TestLogger<CitySystemsResourceDemandConsumer>();
            var consumer = new CitySystemsResourceDemandConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateSystemsDemandMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Deferred classic city systems resource demand",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CitySystemsResourceDemandConsumeAsync_WhenStale_LogsWarning()
        {
            var mediator = new TestSystemsDemandMediator
            {
                Result = new SyncCitySystemsDemandResult(
                    Status: SyncCitySystemsDemandStatus.Stale,
                    OverallDemandPressureIndex: 0.31m,
                    EffectiveTickId: 7,
                    EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc)
            };
            var logger = new TestLogger<CitySystemsResourceDemandConsumer>();
            var consumer = new CitySystemsResourceDemandConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateSystemsDemandMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Warning,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped stale classic city systems resource demand",
                actualString: entry.Message);
        }

        [Fact]
        public async Task CitySystemsResourceDemandConsumeAsync_WhenStateIsNotInitialized_LogsDebug()
        {
            var mediator = new TestSystemsDemandMediator
            {
                Result = new SyncCitySystemsDemandResult(
                    Status: SyncCitySystemsDemandStatus.NotInitialized,
                    OverallDemandPressureIndex: 0m,
                    EffectiveTickId: 0,
                    EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc)
            };
            var logger = new TestLogger<CitySystemsResourceDemandConsumer>();
            var consumer = new CitySystemsResourceDemandConsumer(
                mediator: mediator,
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateSystemsDemandMessage(),
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "stockpiles are not initialized yet",
                actualString: entry.Message);
        }

        private static ClassicCityOperationalBudgetPressureSnapshotV1 CreateBudgetPressureMessage()
        {
            return new ClassicCityOperationalBudgetPressureSnapshotV1(
                CityId: ResourcesInfrastructureTestSupport.CityId,
                Balance: 4500m,
                TotalCityExpenses: 380m,
                MunicipalOperationsExpenses: 120m,
                InfrastructureOperationsExpenses: 95m,
                EmergencyOperationsExpenses: 40m,
                GeneralAvailableAmount: 900m,
                OperationsAvailableAmount: 700m,
                InfrastructureAvailableAmount: 620m,
                HealthcareAvailableAmount: 580m,
                GeneralAuthorizationLevel: "Open",
                OperationsAuthorizationLevel: "Guarded",
                InfrastructureAuthorizationLevel: "Guarded",
                HealthcareAuthorizationLevel: "Protected",
                PressureIndex: 0.36m,
                EffectiveTickId: 11,
                EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc,
                OccurredAtUtc: ResourcesInfrastructureTestSupport.LaterUtc.AddMinutes(1));
        }

        private static ClassicCitySystemsResourceDemandSnapshotV1 CreateSystemsDemandMessage()
        {
            return new ClassicCitySystemsResourceDemandSnapshotV1(
                CityId: ResourcesInfrastructureTestSupport.CityId,
                FuelDemandPressureIndex: 0.2m,
                SparePartsDemandPressureIndex: 0.35m,
                FiltersDemandPressureIndex: 0.18m,
                EmergencyWaterDemandPressureIndex: 0.41m,
                OverallDemandPressureIndex: 0.29m,
                EffectiveTickId: 12,
                EffectiveAtUtc: ResourcesInfrastructureTestSupport.LaterUtc,
                OccurredAtUtc: ResourcesInfrastructureTestSupport.LaterUtc.AddMinutes(2));
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

        private sealed class TestSystemsDemandMediator : IMediator
        {
            public List<SyncCitySystemsDemandCommand> Commands { get; } = [];
            public required SyncCitySystemsDemandResult Result { get; init; }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                SyncCitySystemsDemandCommand command = Assert.IsType<SyncCitySystemsDemandCommand>(request);
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
