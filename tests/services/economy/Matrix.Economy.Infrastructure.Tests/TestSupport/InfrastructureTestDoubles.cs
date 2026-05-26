using Matrix.Economy.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Tests.TestSupport
{
    internal sealed class TestCityEconomyDeletionRepository : ICityEconomyDeletionRepository
    {
        public DateTimeOffset? DeletedAtUtc { get; set; }
        public Guid? RequestedCityId { get; private set; }

        public Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            RequestedCityId = cityId;
            return Task.FromResult(DeletedAtUtc);
        }

        public Task DeleteCityDataAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task RecordAsync(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    internal sealed class TestLogger<T> : ILogger<T>
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
                        arg2: exception),
                    Exception: exception));
        }
    }

    internal sealed record TestLogEntry(
        LogLevel LogLevel,
        string Message,
        Exception? Exception);

    internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    internal sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}
