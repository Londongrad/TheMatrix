using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Tests.TestSupport
{
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

    internal sealed class DictionaryServiceProvider(Dictionary<Type, object> services) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            services.TryGetValue(
                key: serviceType,
                value: out object? service);
            return service;
        }
    }

    internal sealed class TestServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            return new TestServiceScope(serviceProvider);
        }
    }

    internal sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }

    internal sealed class TestServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider => serviceProvider;

        public void Dispose() { }
    }
}
