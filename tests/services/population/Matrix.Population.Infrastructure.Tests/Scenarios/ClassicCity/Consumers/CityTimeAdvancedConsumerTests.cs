using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class CityTimeAdvancedConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenPhaseDoesNotMatchPopulationReaction_DoesNothing()
    {
        var mediator = new TestMediator
        {
            Result = new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.Applied, 5)
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
            Result = new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.Applied, 7)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);
        CityTickPhaseReachedV1 message = CreateMessage(CityTickPhaseV1.PopulationReaction);

        await consumer.ConsumeAsync(message, CancellationToken.None);

        AdvanceCityPopulationCommand command = Assert.Single(mediator.Commands);
        Assert.Equal(message.CityId, command.CityId);
        Assert.Equal(message.TickId, command.TickId);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied city population progression", entry.Message);
    }

    [Fact]
    public async Task ConsumeAsync_WhenAdvanceIsCityArchived_LogsDebug()
    {
        var mediator = new TestMediator
        {
            Result = new AdvanceCityPopulationResult(AdvanceCityPopulationStatus.CityArchived, 0)
        };
        var logger = new TestLogger<CityTimeAdvancedConsumer>();
        var consumer = new CityTimeAdvancedConsumer(mediator, logger);

        await consumer.ConsumeAsync(CreateMessage(CityTickPhaseV1.PopulationReaction), CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("archived", entry.Message);
    }

    private static CityTickPhaseReachedV1 CreateMessage(CityTickPhaseV1 phase)
    {
        return new CityTickPhaseReachedV1(
            CityId: Guid.Parse("825bc802-3315-4387-8ee9-234270789660"),
            FromSimTimeUtc: new DateTimeOffset(2048, 5, 6, 8, 0, 0, TimeSpan.Zero),
            ToSimTimeUtc: new DateTimeOffset(2048, 5, 7, 8, 0, 0, TimeSpan.Zero),
            TickId: 42,
            SpeedMultiplier: 60m,
            TickContext: new CityTickContextV1(
                SimulationId: Guid.Parse("ec4cc0b5-363a-41ec-b088-2432daa26ffd"),
                CityId: Guid.Parse("825bc802-3315-4387-8ee9-234270789660"),
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
        public List<AdvanceCityPopulationCommand> Commands { get; } = [];
        public required AdvanceCityPopulationResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            AdvanceCityPopulationCommand command = Assert.IsType<AdvanceCityPopulationCommand>(request);
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
