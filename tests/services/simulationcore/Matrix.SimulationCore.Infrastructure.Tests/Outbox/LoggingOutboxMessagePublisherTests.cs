using Microsoft.Extensions.Logging;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox;

public sealed class LoggingOutboxMessagePublisherTests
{
    [Fact]
    public async Task PublishAsync_LogsMessageIdAndType()
    {
        var logger = new TestLogger<LoggingOutboxMessagePublisher>();
        var publisher = new LoggingOutboxMessagePublisher(logger);
        Guid messageId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await publisher.PublishAsync(
            messageId: messageId,
            type: "simulationcore.city-created.v1",
            payloadJson: "{\"cityId\":\"ignored\"}",
            cancellationToken: CancellationToken.None);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains(messageId.ToString(), entry.Message);
        Assert.Contains("simulationcore.city-created.v1", entry.Message);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<TestLogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new TestLogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record TestLogEntry(LogLevel LogLevel, string Message);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
