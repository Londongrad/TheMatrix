using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public sealed class CachedPermissionsVersionStore(
        IDistributedCache distributedCache,
        IIdentityPermissionsVersionClient client,
        IOptions<PermissionsVersionOptions> options,
        ILogger<CachedPermissionsVersionStore> logger)
        : IPermissionsVersionStore
    {
        // Чтобы при деградации Redis не получить лог-шторм.
        private static readonly ConcurrentDictionary<string, long> LastLogAt = new();
        private readonly PermissionsVersionOptions _options = options.Value;

        public async Task<int> GetCurrentAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            string cacheKey = PermissionsVersionCacheKeys.ForUser(userId);

            // Порог медленной операции (ранний сигнал деградации Redis без явных exception).
            const int slowMs = 300;

            // 1) Try read from Redis
            try
            {
                var sw = Stopwatch.StartNew();

                string? cached = await distributedCache.GetStringAsync(
                    key: cacheKey,
                    token: cancellationToken);

                sw.Stop();

                if (sw.ElapsedMilliseconds > slowMs &&
                    ShouldLog(
                        key: "pv.redis.read.slow",
                        period: TimeSpan.FromSeconds(30)))
                    logger.LogWarning(
                        message: "Redis read is slow. CacheKey={CacheKey} UserId={UserId} ElapsedMs={ElapsedMs}",
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
                            message: "PermissionsVersion cache hit for user {UserId}. Version={Version}",
                            userId,
                            version);

                    return version;
                }

                if (!string.IsNullOrWhiteSpace(cached))
                {
                    if (ShouldLog(
                            key: "pv.redis.read.invalid",
                            period: TimeSpan.FromMinutes(5)))
                        // RawValue логируем ограниченно, чтобы не засорять; если хочешь — можно ещё и ограничить длину.
                        logger.LogWarning(
                            message:
                            "PermissionsVersion cache value is invalid for user {UserId}. CacheKey={CacheKey} RawValue='{RawValue}'. Falling back to Identity.",
                            userId,
                            cacheKey,
                            cached);
                }
                else
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug(
                            message: "PermissionsVersion cache miss for user {UserId}. Falling back to Identity.",
                            userId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Не считаем это деградацией Redis.
                throw;
            }
            catch (Exception ex)
            {
                // Redis недоступен — просто идём дальше (fallback на Identity), но фиксируем деградацию.
                if (ShouldLog(
                        key: "pv.redis.read.fail",
                        period: TimeSpan.FromSeconds(15)))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to read PermissionsVersion cache (fallback to Identity). UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                        userId,
                        cacheKey,
                        ex.GetType()
                           .FullName);
            }

            // 2) Fallback to Identity
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(
                    message: "Loading PermissionsVersion from Identity for user {UserId}.",
                    userId);

            int currentVersion = await client.GetPermissionsVersionAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            // 3) Try to write to Redis (the best effort)
            try
            {
                var sw = Stopwatch.StartNew();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = GetTtlOrDefault()
                };

                await distributedCache.SetStringAsync(
                    key: cacheKey,
                    value: currentVersion.ToString(CultureInfo.InvariantCulture),
                    options: cacheOptions,
                    token: cancellationToken);

                sw.Stop();

                if (sw.ElapsedMilliseconds > slowMs &&
                    ShouldLog(
                        key: "pv.redis.write.slow",
                        period: TimeSpan.FromSeconds(30)))
                    logger.LogWarning(
                        message:
                        "Redis write is slow. CacheKey={CacheKey} UserId={UserId} ElapsedMs={ElapsedMs} TtlSeconds={TtlSeconds}",
                        cacheKey,
                        userId,
                        sw.ElapsedMilliseconds,
                        _options.CacheTtlSeconds);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Best effort: если Redis лег — не мешаем запросу, но логируем (rate limited).
                if (ShouldLog(
                        key: "pv.redis.write.fail",
                        period: TimeSpan.FromSeconds(15)))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to write PermissionsVersion cache (best effort). UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                        userId,
                        cacheKey,
                        ex.GetType()
                           .FullName);
            }

            return currentVersion;
        }

        private static bool ShouldLog(
            string key,
            TimeSpan period)
        {
            long now = Stopwatch.GetTimestamp();
            long periodTicks = (long)(period.TotalSeconds * Stopwatch.Frequency);

            long last = LastLogAt.GetOrAdd(
                key: key,
                value: 0);
            if (now - last < periodTicks)
                return false;

            LastLogAt[key] = now;
            return true;
        }

        private TimeSpan GetTtlOrDefault()
        {
            // Защита от кривого конфига, чтобы не ставить TTL=0/отрицательный.
            if (_options.CacheTtlSeconds > 0)
                return TimeSpan.FromSeconds(_options.CacheTtlSeconds);

            if (ShouldLog(
                    key: "pv.cache.ttl.invalid",
                    period: TimeSpan.FromMinutes(30)))
                logger.LogWarning(
                    message:
                    "PermissionsVersion cache TTL is invalid (CacheTtlSeconds={CacheTtlSeconds}). Falling back to 300 seconds.",
                    _options.CacheTtlSeconds);

            return TimeSpan.FromSeconds(300);
        }
    }
}
