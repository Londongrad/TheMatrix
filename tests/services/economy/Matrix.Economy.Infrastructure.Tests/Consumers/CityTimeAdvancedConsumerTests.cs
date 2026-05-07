using Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Consumers;

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
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateMessage(CityTickPhaseV1.Projection), CancellationToken.None);

        Assert.Empty(mediator.Commands);
        Assert.Empty(logger.Entries);
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
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);
        CityTickPhaseReachedV1 message = CreateMessage(CityTickPhaseV1.BudgetSettlement);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        AdvanceCityEconomySimulationCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(message.FromSimTimeUtc, command.FromSimTimeUtc);
        Assert.Equal(message.ToSimTimeUtc, command.ToSimTimeUtc);
        Assert.Equal(message.TickId, command.TickId);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied city economy progression", entry.Message);
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
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateMessage(CityTickPhaseV1.BudgetSettlement), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Skipped duplicate city economy progression", entry.Message);
    }

    private static CityTickPhaseReachedV1 CreateMessage(CityTickPhaseV1 phase)
    {
        return new CityTickPhaseReachedV1(
            CityId: Guid.Parse("aa1b729e-b694-4f81-aaf0-88db3757b4af"),
            FromSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            ToSimTimeUtc: new DateTimeOffset(2048, 5, 7, 8, 0, 0, TimeSpan.Zero),
            TickId: 42,
            SpeedMultiplier: 60m,
            TickContext: new CityTickContextV1(
                SimulationId: Guid.Parse("176c7256-66f0-4a7f-9378-260ddf3d9940"),
                CityId: Guid.Parse("aa1b729e-b694-4f81-aaf0-88db3757b4af"),
                SimulationKind: "classic-city",
                TickId: 42,
                EffectiveSimTimeUtc: new DateTimeOffset(2048, 5, 7, 8, 0, 0, TimeSpan.Zero),
                Phase: phase,
                ModelVersion: 1,
                CausationId: "tick:42",
                CorrelationId: "corr:42"),
            OccurredOnUtc: new DateTime(2048, 5, 6, 8, 1, 0, DateTimeKind.Utc));
    }

    private sealed class TestMediator : IMediator
    {
        public List<AdvanceCityEconomySimulationCommand> Commands { get; } = [];

        public required AdvanceCityEconomySimulationResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            AdvanceCityEconomySimulationCommand command = Assert.IsType<AdvanceCityEconomySimulationCommand>(request);
            Commands.Add(command);
            return Task.FromResult((TResponse)(object)Result);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            throw new NotSupportedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
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

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            throw new NotSupportedException();
        }
    }
}
