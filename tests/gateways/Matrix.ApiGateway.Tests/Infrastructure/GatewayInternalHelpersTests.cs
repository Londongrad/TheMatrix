using System.Reflection;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.DownstreamClients.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.ApiGateway.Tests.Infrastructure;

public sealed class GatewayInternalHelpersTests
{
    [Fact]
    public void AuthorizationCacheKeys_BuildExpectedKeys()
    {
        Guid userId = Guid.Parse("098a2eb5-fd4f-413b-8dfc-28776b7f0cc9");

        Assert.Equal($"pv:{userId:N}", AuthorizationCacheKeys.PermissionsVersion(userId));
        Assert.Equal($"pv:stale:{userId:N}", AuthorizationCacheKeys.PermissionsVersionStale(userId));
        Assert.Equal($"ac:{userId:N}:7", AuthorizationCacheKeys.AuthContext(userId, 7));
        Assert.Equal("pv:default-user-access", AuthorizationCacheKeys.DefaultUserAccessVersion());
        Assert.Equal("pv:stale:default-user-access", AuthorizationCacheKeys.DefaultUserAccessVersionStale());
    }

    [Fact]
    public void DownstreamServiceNames_ExposeExpectedConstants()
    {
        Assert.Equal("SimulationCore", DownstreamServiceNames.SimulationCore);
        Assert.Equal("SimulationSystems", DownstreamServiceNames.SimulationSystems);
        Assert.Equal("Economy", DownstreamServiceNames.Economy);
        Assert.Equal("Resources", DownstreamServiceNames.Resources);
        Assert.Equal("Population", DownstreamServiceNames.Population);
        Assert.Equal("Identity", DownstreamServiceNames.Identity);
    }

    [Fact]
    public void CacheTtlPolicy_WhenTtlIsPositive_ReturnsConfiguredDuration()
    {
        TimeSpan ttl = InvokeCacheTtlPolicy(
            ttlSeconds: 45,
            defaultTtlSeconds: 1800,
            logKey: $"ttl-positive-{Guid.NewGuid():N}",
            cacheName: "PermissionsVersion",
            logger: null);

        Assert.Equal(TimeSpan.FromSeconds(45), ttl);
    }

    [Fact]
    public void CacheTtlPolicy_WhenTtlIsInvalid_FallsBackAndLogsWarning()
    {
        RecordingLogger logger = new();

        TimeSpan ttl = InvokeCacheTtlPolicy(
            ttlSeconds: 0,
            defaultTtlSeconds: 1800,
            logKey: $"ttl-invalid-{Guid.NewGuid():N}",
            cacheName: "PermissionsVersion",
            logger: logger);

        Assert.Equal(TimeSpan.FromSeconds(1800), ttl);
        Assert.Single(logger.Messages);
        Assert.Contains("PermissionsVersion cache TTL is invalid", logger.Messages[0]);
    }

    [Fact]
    public void LogRateLimiter_SuppressesRepeatedLogsWithinWindow()
    {
        string key = $"log-rate-{Guid.NewGuid():N}";

        bool first = InvokeLogRateLimiter(key, TimeSpan.FromMilliseconds(150));
        bool second = InvokeLogRateLimiter(key, TimeSpan.FromMilliseconds(150));

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public void LogRateLimiter_AllowsLogAgainAfterWindowExpires()
    {
        string key = $"log-rate-expire-{Guid.NewGuid():N}";

        Assert.True(InvokeLogRateLimiter(key, TimeSpan.FromMilliseconds(60)));
        Thread.Sleep(100);
        Assert.True(InvokeLogRateLimiter(key, TimeSpan.FromMilliseconds(60)));
    }

    private static TimeSpan InvokeCacheTtlPolicy(
        int ttlSeconds,
        int defaultTtlSeconds,
        string logKey,
        string cacheName,
        object? logger)
    {
        Type type = typeof(DownstreamServiceNames).Assembly.GetType(
                        "Matrix.ApiGateway.Infrastructure.Caching.CacheTtlPolicy",
                        throwOnError: true)!
                    ?? throw new InvalidOperationException("CacheTtlPolicy type was not found.");
        MethodInfo method = type.GetMethod(
                                "GetTtlOrDefault",
                                BindingFlags.Static | BindingFlags.NonPublic)!
                            ?? throw new InvalidOperationException("CacheTtlPolicy.GetTtlOrDefault method was not found.");

        return (TimeSpan)(method.Invoke(null, [ttlSeconds, defaultTtlSeconds, logKey, cacheName, logger])!);
    }

    private static bool InvokeLogRateLimiter(string key, TimeSpan period)
    {
        Type type = typeof(DownstreamServiceNames).Assembly.GetType(
                        "Matrix.ApiGateway.Infrastructure.Logging.LogRateLimiter",
                        throwOnError: true)!
                    ?? throw new InvalidOperationException("LogRateLimiter type was not found.");
        MethodInfo method = type.GetMethod(
                                "ShouldLog",
                                BindingFlags.Static | BindingFlags.Public)!
                            ?? throw new InvalidOperationException("LogRateLimiter.ShouldLog method was not found.");

        return (bool)(method.Invoke(null, [key, period])!);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
