using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Population.Infrastructure.Tests.TestSupport;
using Matrix.SimulationCore.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Consumers;

public sealed class CityEnvironmentChangedConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenSyncIsApplied_SendsMappedCommandAndLogsInformation()
    {
        var mediator = new TestMediator
        {
            Result = new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Applied)
        };
        var logger = new TestLogger<CityEnvironmentChangedConsumer>();
        var consumer = new CityEnvironmentChangedConsumer(mediator, logger);
        CityEnvironmentChangedV1 message = new(
            CityId: Guid.Parse("e01d80c0-4948-4805-bbf9-3ec028d6919c"),
            PreviousEnvironment: null,
            CurrentEnvironment: new CityEnvironmentV1("Temperate", "Northern", 180),
            OccurredOnUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero));

        await consumer.ConsumeAsync(message, CancellationToken.None);

        ApplyCityEnvironmentSyncCommand command = Assert.Single(mediator.Commands);
        Assert.Equal("Temperate", command.ClimateZone);
        Assert.Equal("Northern", command.Hemisphere);
        Assert.Equal(180, command.UtcOffsetMinutes);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied city environment sync", entry.Message);
    }

    [Fact]
    public async Task ConsumeAsync_WhenSyncIsStale_LogsWarning()
    {
        var mediator = new TestMediator
        {
            Result = new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Stale)
        };
        var logger = new TestLogger<CityEnvironmentChangedConsumer>();
        var consumer = new CityEnvironmentChangedConsumer(mediator, logger);

        await consumer.ConsumeAsync(
            new CityEnvironmentChangedV1(
                CityId: Guid.Parse("e01d80c0-4948-4805-bbf9-3ec028d6919c"),
                PreviousEnvironment: null,
                CurrentEnvironment: new CityEnvironmentV1("Temperate", "Northern", 180),
                OccurredOnUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
        Assert.Contains("stale", entry.Message);
    }

    private sealed class TestMediator : IMediator
    {
        public List<ApplyCityEnvironmentSyncCommand> Commands { get; } = [];
        public required SyncCityEnvironmentResult Result { get; init; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ApplyCityEnvironmentSyncCommand command = Assert.IsType<ApplyCityEnvironmentSyncCommand>(request);
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
