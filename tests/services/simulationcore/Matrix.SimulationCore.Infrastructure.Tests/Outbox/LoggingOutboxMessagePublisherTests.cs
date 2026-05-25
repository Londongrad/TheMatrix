using Matrix.SimulationCore.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.SimulationCore.Infrastructure.Tests.Outbox
{
    public sealed class LoggingOutboxMessagePublisherTests
    {
        [Fact]
        public async Task PublishAsync_LogsMessageIdAndType()
        {
            var logger = new TestLogger<LoggingOutboxMessagePublisher>();
            var publisher = new LoggingOutboxMessagePublisher(logger);
            var messageId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            await publisher.PublishAsync(
                messageId: messageId,
                type: "simulationcore.city-created.v1",
                payloadJson: "{\"cityId\":\"ignored\"}",
                cancellationToken: CancellationToken.None);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: messageId.ToString(),
                actualString: entry.Message);
            Assert.Contains(
                expectedSubstring: "simulationcore.city-created.v1",
                actualString: entry.Message);
        }

        private sealed class TestLogger<T> : ILogger<T>
        {
            public List<TestLogEntry> Entries { get; } = [];

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add(
                    new TestLogEntry(
                        LogLevel: logLevel,
                        Message: formatter(
                            arg1: state,
                            arg2: exception)));
            }
        }

        private sealed record TestLogEntry(
            LogLevel LogLevel,
            string Message);

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
