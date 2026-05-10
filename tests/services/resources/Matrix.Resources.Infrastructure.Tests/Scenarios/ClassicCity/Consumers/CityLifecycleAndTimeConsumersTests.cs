using Matrix.Resources.Application.Scenarios.ClassicCity;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Resources.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Resources.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class CityLifecycleAndTimeConsumersTests
{
    [Fact]
    public async Task CityCreatedConsumeAsync_WhenSimulationKindDoesNotMatch_DoesNothingAndLogsDebug()
    {
        var mediator = new TestSeedMediator
        {
            Result = new SeedCityStockpilesResult(
                Status: SeedCityStockpilesStatus.Applied,
                CityId: Guid.Empty,
                SupplyStressIndex: 0m,
                EmergencyRationingEnabled: false)
        };
        var logger = new TestLogger<CityCreatedConsumer>();
        var consumer = new CityCreatedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateCityCreatedMessage("Sandbox"), CancellationToken.None);

        Assert.Empty(mediator.Commands);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Ignored city-created event", entry.Message);
    }

    [Fact]
    public async Task CityCreatedConsumeAsync_WhenSeedIsApplied_SendsCommandAndLogsInformation()
    {
        CityCreatedV1 message = CreateCityCreatedMessage(ClassicCityScenario.Name);
        var mediator = new TestSeedMediator
        {
            Result = new SeedCityStockpilesResult(
                Status: SeedCityStockpilesStatus.Applied,
                CityId: message.CityId,
                SupplyStressIndex: 0.18m,
                EmergencyRationingEnabled: false)
        };
        var logger = new TestLogger<CityCreatedConsumer>();
        var consumer = new CityCreatedConsumer(mediator, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        SeedCityStockpilesCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(message.CreatedAtUtc, command.CreatedAtUtc);
        Assert.Equal(message.SimulationKind, command.SimulationKind);
        Assert.Equal(message.DevelopmentLevel, command.DevelopmentLevel);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Initialized classic city stockpiles", entry.Message);
    }

    [Fact]
    public async Task CityCreatedConsumeAsync_WhenSeedIsDuplicate_LogsDebug()
    {
        var mediator = new TestSeedMediator
        {
            Result = new SeedCityStockpilesResult(
                Status: SeedCityStockpilesStatus.Duplicate,
                CityId: ResourcesInfrastructureTestSupport.CityId,
                SupplyStressIndex: 0m,
                EmergencyRationingEnabled: false)
        };
        var logger = new TestLogger<CityCreatedConsumer>();
        var consumer = new CityCreatedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateCityCreatedMessage(ClassicCityScenario.Name), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Skipped duplicate classic city stockpile seed", entry.Message);
    }

    [Fact]
    public async Task CityTimeAdvancedConsumeAsync_WhenPhaseDoesNotMatchResourceSettlement_DoesNothing()
    {
        var mediator = new TestAdvanceMediator
        {
            Result = new AdvanceCityStockpilesResult(
                Status: AdvanceCityStockpilesStatus.Applied,
                CityId: ResourcesInfrastructureTestSupport.CityId,
                ProcessedSimMinutes: 60,
                SupplyStressIndex: 0.2m,
                FuelStockLevelIndex: 0.8m,
                FoodStockLevelIndex: 0.7m,
                EmergencyWaterStockLevelIndex: 0.9m)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateTickMessage(CityTickPhaseV1.Projection), CancellationToken.None);

        Assert.Empty(mediator.Commands);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task CityTimeAdvancedConsumeAsync_WhenAdvanceIsApplied_SendsCommandAndLogsInformation()
    {
        CityTickPhaseReachedV1 message = CreateTickMessage(CityTickPhaseV1.ResourceSettlement);
        var mediator = new TestAdvanceMediator
        {
            Result = new AdvanceCityStockpilesResult(
                Status: AdvanceCityStockpilesStatus.Applied,
                CityId: message.CityId,
                ProcessedSimMinutes: 120,
                SupplyStressIndex: 0.33m,
                FuelStockLevelIndex: 0.61m,
                FoodStockLevelIndex: 0.58m,
                EmergencyWaterStockLevelIndex: 0.73m)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        AdvanceCityStockpilesCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(message.FromSimTimeUtc, command.FromSimTimeUtc);
        Assert.Equal(message.ToSimTimeUtc, command.ToSimTimeUtc);
        Assert.Equal(message.TickId, command.TickId);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied classic city stockpile time progression", entry.Message);
    }

    [Fact]
    public async Task CityTimeAdvancedConsumeAsync_WhenAdvanceIsOutOfOrder_LogsDebug()
    {
        var mediator = new TestAdvanceMediator
        {
            Result = new AdvanceCityStockpilesResult(
                Status: AdvanceCityStockpilesStatus.OutOfOrder,
                CityId: ResourcesInfrastructureTestSupport.CityId,
                ProcessedSimMinutes: 0,
                SupplyStressIndex: 0m,
                FuelStockLevelIndex: 0m,
                FoodStockLevelIndex: 0m,
                EmergencyWaterStockLevelIndex: 0m)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateTickMessage(CityTickPhaseV1.ResourceSettlement), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Skipped out-of-order classic city stockpile time progression", entry.Message);
    }

    [Fact]
    public async Task CityTimeAdvancedConsumeAsync_WhenAdvanceIsDuplicate_LogsDebug()
    {
        var mediator = new TestAdvanceMediator
        {
            Result = new AdvanceCityStockpilesResult(
                Status: AdvanceCityStockpilesStatus.Duplicate,
                CityId: ResourcesInfrastructureTestSupport.CityId,
                ProcessedSimMinutes: 0,
                SupplyStressIndex: 0m,
                FuelStockLevelIndex: 0m,
                FoodStockLevelIndex: 0m,
                EmergencyWaterStockLevelIndex: 0m)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateTickMessage(CityTickPhaseV1.ResourceSettlement), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Skipped duplicate classic city stockpile time progression", entry.Message);
    }

    [Fact]
    public async Task CityTimeAdvancedConsumeAsync_WhenStateIsNotInitialized_LogsDebug()
    {
        var mediator = new TestAdvanceMediator
        {
            Result = new AdvanceCityStockpilesResult(
                Status: AdvanceCityStockpilesStatus.NotInitialized,
                CityId: ResourcesInfrastructureTestSupport.CityId,
                ProcessedSimMinutes: 0,
                SupplyStressIndex: 0m,
                FuelStockLevelIndex: 0m,
                FoodStockLevelIndex: 0m,
                EmergencyWaterStockLevelIndex: 0m)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateTickMessage(CityTickPhaseV1.ResourceSettlement), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("state is not initialized yet", entry.Message);
    }

    private static CityCreatedV1 CreateCityCreatedMessage(string simulationKind)
    {
        return new CityCreatedV1(
            CityId: ResourcesInfrastructureTestSupport.CityId,
            Name: "Northreach",
            SimulationKind: simulationKind,
            CreatedAtUtc: ResourcesInfrastructureTestSupport.CreatedAtUtc,
            DevelopmentLevel: "advanced",
            EconomyProfile: "balanced");
    }

    private static CityTickPhaseReachedV1 CreateTickMessage(CityTickPhaseV1 phase)
    {
        return new CityTickPhaseReachedV1(
            CityId: ResourcesInfrastructureTestSupport.CityId,
            FromSimTimeUtc: ResourcesInfrastructureTestSupport.CreatedAtUtc,
            ToSimTimeUtc: ResourcesInfrastructureTestSupport.LaterUtc,
            TickId: 9,
            SpeedMultiplier: 60m,
            TickContext: new CityTickContextV1(
                SimulationId: Guid.Parse("94c5dc18-f29b-4055-8b79-fcd49ca62b76"),
                CityId: ResourcesInfrastructureTestSupport.CityId,
                SimulationKind: ClassicCityScenario.Name,
                TickId: 9,
                EffectiveSimTimeUtc: ResourcesInfrastructureTestSupport.LaterUtc,
                Phase: phase,
                ModelVersion: 1,
                CausationId: "tick:9",
                CorrelationId: "corr:9"),
            OccurredOnUtc: ResourcesInfrastructureTestSupport.LaterUtc.UtcDateTime);
    }

    private sealed class TestSeedMediator : IMediator
    {
        public List<SeedCityStockpilesCommand> Commands { get; } = [];
        public required SeedCityStockpilesResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SeedCityStockpilesCommand command = Assert.IsType<SeedCityStockpilesCommand>(request);
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

    private sealed class TestAdvanceMediator : IMediator
    {
        public List<AdvanceCityStockpilesCommand> Commands { get; } = [];
        public required AdvanceCityStockpilesResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            AdvanceCityStockpilesCommand command = Assert.IsType<AdvanceCityStockpilesCommand>(request);
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
