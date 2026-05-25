using System.Reflection;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.DownstreamClients.Common;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.ApiGateway.Tests.Infrastructure
{
    public sealed class GatewayInternalHelpersTests
    {
        [Fact]
        public void AuthorizationCacheKeys_BuildExpectedKeys()
        {
            var userId = Guid.Parse("098a2eb5-fd4f-413b-8dfc-28776b7f0cc9");

            Assert.Equal(
                expected: $"pv:{userId:N}",
                actual: AuthorizationCacheKeys.PermissionsVersion(userId));
            Assert.Equal(
                expected: $"pv:stale:{userId:N}",
                actual: AuthorizationCacheKeys.PermissionsVersionStale(userId));
            Assert.Equal(
                expected: $"ac:{userId:N}:7",
                actual: AuthorizationCacheKeys.AuthContext(
                    userId: userId,
                    permissionsVersion: 7));
            Assert.Equal(
                expected: "pv:default-user-access",
                actual: AuthorizationCacheKeys.DefaultUserAccessVersion());
            Assert.Equal(
                expected: "pv:stale:default-user-access",
                actual: AuthorizationCacheKeys.DefaultUserAccessVersionStale());
        }

        [Fact]
        public void DownstreamServiceNames_ExposeExpectedConstants()
        {
            Assert.Equal(
                expected: "SimulationCore",
                actual: DownstreamServiceNames.SimulationCore);
            Assert.Equal(
                expected: "SimulationSystems",
                actual: DownstreamServiceNames.SimulationSystems);
            Assert.Equal(
                expected: "Economy",
                actual: DownstreamServiceNames.Economy);
            Assert.Equal(
                expected: "Resources",
                actual: DownstreamServiceNames.Resources);
            Assert.Equal(
                expected: "Population",
                actual: DownstreamServiceNames.Population);
            Assert.Equal(
                expected: "Identity",
                actual: DownstreamServiceNames.Identity);
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

            Assert.Equal(
                expected: TimeSpan.FromSeconds(45),
                actual: ttl);
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

            Assert.Equal(
                expected: TimeSpan.FromSeconds(1800),
                actual: ttl);
            Assert.Single(logger.Messages);
            Assert.Contains(
                expectedSubstring: "PermissionsVersion cache TTL is invalid",
                actualString: logger.Messages[0]);
        }

        [Fact]
        public void LogRateLimiter_SuppressesRepeatedLogsWithinWindow()
        {
            string key = $"log-rate-{Guid.NewGuid():N}";

            bool first = InvokeLogRateLimiter(
                key: key,
                period: TimeSpan.FromMilliseconds(150));
            bool second = InvokeLogRateLimiter(
                key: key,
                period: TimeSpan.FromMilliseconds(150));

            Assert.True(first);
            Assert.False(second);
        }

        [Fact]
        public void LogRateLimiter_AllowsLogAgainAfterWindowExpires()
        {
            string key = $"log-rate-expire-{Guid.NewGuid():N}";

            Assert.True(
                InvokeLogRateLimiter(
                    key: key,
                    period: TimeSpan.FromMilliseconds(60)));
            Thread.Sleep(100);
            Assert.True(
                InvokeLogRateLimiter(
                    key: key,
                    period: TimeSpan.FromMilliseconds(60)));
        }

        private static TimeSpan InvokeCacheTtlPolicy(
            int ttlSeconds,
            int defaultTtlSeconds,
            string logKey,
            string cacheName,
            object? logger)
        {
            Type type = typeof(DownstreamServiceNames).Assembly.GetType(
                            name: "Matrix.ApiGateway.Infrastructure.Caching.CacheTtlPolicy",
                            throwOnError: true)! ??
                        throw new InvalidOperationException("CacheTtlPolicy type was not found.");
            MethodInfo method = type.GetMethod(
                                    name: "GetTtlOrDefault",
                                    bindingAttr: BindingFlags.Static | BindingFlags.NonPublic)! ??
                                throw new InvalidOperationException(
                                    "CacheTtlPolicy.GetTtlOrDefault method was not found.");

            return (TimeSpan)method.Invoke(
                obj: null,
                parameters:
                [
                    ttlSeconds,
                    defaultTtlSeconds,
                    logKey,
                    cacheName,
                    logger
                ])!;
        }

        private static bool InvokeLogRateLimiter(
            string key,
            TimeSpan period)
        {
            Type type = typeof(DownstreamServiceNames).Assembly.GetType(
                            name: "Matrix.ApiGateway.Infrastructure.Logging.LogRateLimiter",
                            throwOnError: true)! ??
                        throw new InvalidOperationException("LogRateLimiter type was not found.");
            MethodInfo method = type.GetMethod(
                                    name: "ShouldLog",
                                    bindingAttr: BindingFlags.Static | BindingFlags.Public)! ??
                                throw new InvalidOperationException("LogRateLimiter.ShouldLog method was not found.");

            return (bool)method.Invoke(
                obj: null,
                parameters:
                [
                    key,
                    period
                ])!;
        }

        private sealed class RecordingLogger : ILogger
        {
            public List<string> Messages { get; } = [];

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
                Messages.Add(
                    formatter(
                        arg1: state,
                        arg2: exception));
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose() { }
            }
        }
    }
}
