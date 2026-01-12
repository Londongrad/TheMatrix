using Matrix.ApiGateway.Infrastructure.Logging;

namespace Matrix.ApiGateway.Infrastructure.Caching
{
    internal static class CacheTtlPolicy
    {
        internal static TimeSpan GetTtlOrDefault(
            int ttlSeconds,
            int defaultTtlSeconds,
            string logKey,
            string cacheName,
            ILogger? logger = null)
        {
            if (ttlSeconds > 0)
                return TimeSpan.FromSeconds(ttlSeconds);

            if (logger != null &&
                LogRateLimiter.ShouldLog(
                    key: logKey,
                    period: TimeSpan.FromMinutes(30)))
            {
                logger.LogWarning(
                    "{CacheName} cache TTL is invalid (CacheTtlSeconds={CacheTtlSeconds}). Falling back to {DefaultTtlSeconds} seconds.",
                    cacheName,
                    ttlSeconds,
                    defaultTtlSeconds);
            }

            return TimeSpan.FromSeconds(defaultTtlSeconds);
        }
    }
}
