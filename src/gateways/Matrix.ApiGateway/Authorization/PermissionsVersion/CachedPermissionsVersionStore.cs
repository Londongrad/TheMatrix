using System.Diagnostics;
using System.Globalization;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.Infrastructure.Caching;
using Matrix.ApiGateway.Infrastructure.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public sealed class CachedPermissionsVersionStore(
        IDistributedCache distributedCache,
        IIdentityInternalUsersClient client,
        IOptions<PermissionsVersionOptions> options,
        ILogger<CachedPermissionsVersionStore> logger)
        : IPermissionsVersionStore
    {
        private const int DefaultCacheTtlSeconds = 1800;
        private readonly PermissionsVersionOptions _options = options.Value;

        public async Task<int> GetCurrentAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            string cacheKey = AuthorizationCacheKeys.PermissionsVersion(userId);
            string staleCacheKey = AuthorizationCacheKeys.PermissionsVersionStale(userId);

            int? cachedVersion = await TryGetCachedVersionAsync(
                cacheKey: cacheKey,
                userId: userId,
                cacheTier: "fresh",
                logOnMiss: true,
                cancellationToken: cancellationToken);

            if (cachedVersion.HasValue)
                return cachedVersion.Value;

            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(
                    message: "Loading PermissionsVersion from Identity for user {UserId}.",
                    userId);

            int currentVersion = await LoadFromIdentityOrStaleAsync(
                userId: userId,
                staleCacheKey: staleCacheKey,
                cancellationToken: cancellationToken);

            await WriteCachedVersionAsync(
                cacheKey: cacheKey,
                userId: userId,
                cacheName: "PermissionsVersion",
                ttlSeconds: _options.CacheTtlSeconds,
                ttlLogKey: RedisCacheLogKeys.PvCacheTtlInvalid,
                version: currentVersion,
                cancellationToken: cancellationToken);

            if (_options.AllowStaleCacheOnIdentityFailure)
                await WriteCachedVersionAsync(
                    cacheKey: staleCacheKey,
                    userId: userId,
                    cacheName: "PermissionsVersionStale",
                    ttlSeconds: _options.StaleCacheTtlSeconds,
                    ttlLogKey: RedisCacheLogKeys.PvStaleCacheTtlInvalid,
                    version: currentVersion,
                    cancellationToken: cancellationToken);

            return currentVersion;
        }

        private async Task<int> LoadFromIdentityOrStaleAsync(
            Guid userId,
            string staleCacheKey,
            CancellationToken cancellationToken)
        {
            try
            {
                return await client.GetPermissionsVersionAsync(
                    userId: userId,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_options.AllowStaleCacheOnIdentityFailure)
                {
                    int? staleVersion = await TryGetCachedVersionAsync(
                        cacheKey: staleCacheKey,
                        userId: userId,
                        cacheTier: "stale",
                        logOnMiss: false,
                        cancellationToken: cancellationToken);

                    if (staleVersion.HasValue)
                    {
                        if (LogRateLimiter.ShouldLog(
                                key: LogKeys.IdentityFallbackToStale,
                                period: CacheLoggingDefaults.FailPeriod))
                            logger.LogWarning(
                                exception: ex,
                                message:
                                "Failed to load PermissionsVersion from Identity. Falling back to stale cache. UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                                userId,
                                staleCacheKey,
                                ex.GetType()
                                   .FullName);

                        return staleVersion.Value;
                    }
                }

                if (LogRateLimiter.ShouldLog(
                        key: LogKeys.IdentityUnavailable,
                        period: CacheLoggingDefaults.FailPeriod))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to load PermissionsVersion from Identity and no fallback value is available. UserId={UserId} ExceptionType={ExceptionType}",
                        userId,
                        ex.GetType()
                           .FullName);

                throw new PermissionsVersionUnavailableException(
                    userId: userId,
                    innerException: ex);
            }
        }

        private async Task<int?> TryGetCachedVersionAsync(
            string cacheKey,
            Guid userId,
            string cacheTier,
            bool logOnMiss,
            CancellationToken cancellationToken)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                string? cached = await distributedCache.GetStringAsync(
                    key: cacheKey,
                    token: cancellationToken);

                sw.Stop();

                if (sw.ElapsedMilliseconds > CacheLoggingDefaults.SlowOperationMs &&
                    LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.PvRedisReadSlow,
                        period: CacheLoggingDefaults.SlowPeriod))
                    logger.LogWarning(
                        message:
                        "Redis read is slow. CacheTier={CacheTier} CacheKey={CacheKey} UserId={UserId} ElapsedMs={ElapsedMs}",
                        cacheTier,
                        cacheKey,
                        userId,
                        sw.ElapsedMilliseconds);

                if (!string.IsNullOrWhiteSpace(cached) &&
                    int.TryParse(
                        s: cached,
                        style: NumberStyles.Integer,
                        provider: CultureInfo.InvariantCulture,
                        result: out int version))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug(
                            message: "PermissionsVersion {CacheTier} cache hit for user {UserId}. Version={Version}",
                            cacheTier,
                            userId,
                            version);

                    return version;
                }

                if (!string.IsNullOrWhiteSpace(cached))
                {
                    if (LogRateLimiter.ShouldLog(
                            key: RedisCacheLogKeys.PvRedisReadInvalid,
                            period: CacheLoggingDefaults.InvalidPeriod))
                        logger.LogWarning(
                            message:
                            "PermissionsVersion {CacheTier} cache value is invalid for user {UserId}. CacheKey={CacheKey} RawValue='{RawValue}'.",
                            cacheTier,
                            userId,
                            cacheKey,
                            cached);
                }
                else
                    if (logOnMiss &&
                        logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug(
                            message: "PermissionsVersion {CacheTier} cache miss for user {UserId}.",
                            cacheTier,
                            userId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.PvRedisReadFail,
                        period: CacheLoggingDefaults.FailPeriod))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to read PermissionsVersion {CacheTier} cache. UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                        cacheTier,
                        userId,
                        cacheKey,
                        ex.GetType()
                           .FullName);
            }

            return null;
        }

        private async Task WriteCachedVersionAsync(
            string cacheKey,
            Guid userId,
            string cacheName,
            int ttlSeconds,
            string ttlLogKey,
            int version,
            CancellationToken cancellationToken)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtlPolicy.GetTtlOrDefault(
                        ttlSeconds: ttlSeconds,
                        defaultTtlSeconds: DefaultCacheTtlSeconds,
                        logKey: ttlLogKey,
                        cacheName: cacheName,
                        logger: logger)
                };

                await distributedCache.SetStringAsync(
                    key: cacheKey,
                    value: version.ToString(CultureInfo.InvariantCulture),
                    options: cacheOptions,
                    token: cancellationToken);

                sw.Stop();

                if (sw.ElapsedMilliseconds > CacheLoggingDefaults.SlowOperationMs &&
                    LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.PvRedisWriteSlow,
                        period: CacheLoggingDefaults.SlowPeriod))
                    logger.LogWarning(
                        message:
                        "Redis write is slow. CacheName={CacheName} CacheKey={CacheKey} UserId={UserId} ElapsedMs={ElapsedMs} TtlSeconds={TtlSeconds}",
                        cacheName,
                        cacheKey,
                        userId,
                        sw.ElapsedMilliseconds,
                        ttlSeconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.PvRedisWriteFail,
                        period: CacheLoggingDefaults.FailPeriod))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to write {CacheName} cache (best effort). UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                        cacheName,
                        userId,
                        cacheKey,
                        ex.GetType()
                           .FullName);
            }
        }

        private static class LogKeys
        {
            internal const string IdentityFallbackToStale = "pv.identity.fallback.stale";
            internal const string IdentityUnavailable = "pv.identity.unavailable";
        }
    }
}
