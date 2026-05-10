using Microsoft.Extensions.Logging;

namespace Matrix.Resources.Infrastructure.Tests.TestSupport;

internal sealed class TestLogger<T> : ILogger<T>
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
        Entries.Add(new TestLogEntry(logLevel, formatter(state, exception), exception));
    }
}

internal sealed record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception);

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    public void Dispose()
    {
    }
}
