using Matrix.ApiGateway.Infrastructure.Logging;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    internal static class PermissionsVersionCachePolicy
    {
        internal const int DefaultTtlSeconds = 300;

        internal static TimeSpan GetTtlOrDefault(
            int ttlSeconds,
            ILogger? logger = null)
        {
            if (ttlSeconds > 0)
                return TimeSpan.FromSeconds(ttlSeconds);

            // Защита от кривого конфига, чтобы не ставить TTL=0/отрицательный.
            if (logger != null &&
                LogRateLimiter.ShouldLog(
                    key: "pv.cache.ttl.invalid",
                    period: TimeSpan.FromMinutes(30)))
                logger.LogWarning(
                    message:
                    "PermissionsVersion cache TTL is invalid (CacheTtlSeconds={CacheTtlSeconds}). Falling back to {DefaultTtlSeconds} seconds.",
                    ttlSeconds,
                    DefaultTtlSeconds);

            return TimeSpan.FromSeconds(DefaultTtlSeconds);
        }
    }
}
