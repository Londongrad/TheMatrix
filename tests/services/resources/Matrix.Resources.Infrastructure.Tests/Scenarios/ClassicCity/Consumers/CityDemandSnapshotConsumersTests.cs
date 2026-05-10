using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Resources;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCityOperationalBudgetPressure;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SyncCitySystemsDemand;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

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
        var consumer = new CityOperationalBudgetPressureConsumer(mediator, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        SyncCityOperationalBudgetPressureCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(message.Balance, command.Balance);
        Assert.Equal(message.MunicipalOperationsExpenses, command.MunicipalOperationsExpenses);
        Assert.Equal(message.GeneralAvailableAmount, command.GeneralAvailableAmount);
        Assert.Equal(message.OperationsAvailableAmount, command.OperationsAvailableAmount);
        Assert.Equal(message.InfrastructureAvailableAmount, command.InfrastructureAvailableAmount);
        Assert.Equal(message.HealthcareAvailableAmount, command.HealthcareAvailableAmount);
        Assert.Equal(message.GeneralAuthorizationLevel, command.GeneralAuthorizationLevel);
        Assert.Equal(message.OperationsAuthorizationLevel, command.OperationsAuthorizationLevel);
        Assert.Equal(message.InfrastructureAuthorizationLevel, command.InfrastructureAuthorizationLevel);
        Assert.Equal(message.HealthcareAuthorizationLevel, command.HealthcareAuthorizationLevel);
        Assert.Equal(message.PressureIndex, command.PressureIndex);
        Assert.Equal(message.EffectiveTickId, command.EffectiveTickId);
        Assert.Equal(message.EffectiveAtUtc, command.EffectiveAtUtc);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied classic city operational budget pressure", entry.Message);
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
        var consumer = new CityOperationalBudgetPressureConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateBudgetPressureMessage(), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
        Assert.Contains("Skipped stale classic city operational budget pressure", entry.Message);
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
        var consumer = new CityOperationalBudgetPressureConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateBudgetPressureMessage(), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("stockpiles are not initialized yet", entry.Message);
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
        var consumer = new CitySystemsResourceDemandConsumer(mediator, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        SyncCitySystemsDemandCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(message.FuelDemandPressureIndex, command.FuelDemandPressureIndex);
        Assert.Equal(message.SparePartsDemandPressureIndex, command.SparePartsDemandPressureIndex);
        Assert.Equal(message.FiltersDemandPressureIndex, command.FiltersDemandPressureIndex);
        Assert.Equal(message.EmergencyWaterDemandPressureIndex, command.EmergencyWaterDemandPressureIndex);
        Assert.Equal(message.OverallDemandPressureIndex, command.OverallDemandPressureIndex);
        Assert.Equal(message.EffectiveTickId, command.EffectiveTickId);
        Assert.Equal(message.EffectiveAtUtc, command.EffectiveAtUtc);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied classic city systems resource demand", entry.Message);
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
        var consumer = new CitySystemsResourceDemandConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateSystemsDemandMessage(), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Deferred classic city systems resource demand", entry.Message);
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
        var consumer = new CitySystemsResourceDemandConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateSystemsDemandMessage(), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
        Assert.Contains("Skipped stale classic city systems resource demand", entry.Message);
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
        var consumer = new CitySystemsResourceDemandConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateSystemsDemandMessage(), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("stockpiles are not initialized yet", entry.Message);
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

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SyncCityOperationalBudgetPressureCommand command = Assert.IsType<SyncCityOperationalBudgetPressureCommand>(request);
            Commands.Add(command);
            return Task.FromResult((TResponse)(object)Result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();
    }

    private sealed class TestSystemsDemandMediator : IMediator
    {
        public List<SyncCitySystemsDemandCommand> Commands { get; } = [];
        public required SyncCitySystemsDemandResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SyncCitySystemsDemandCommand command = Assert.IsType<SyncCitySystemsDemandCommand>(request);
            Commands.Add(command);
            return Task.FromResult((TResponse)(object)Result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotSupportedException();
    }
}
