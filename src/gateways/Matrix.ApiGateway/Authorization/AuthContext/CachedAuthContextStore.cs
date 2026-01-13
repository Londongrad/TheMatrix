using System.Diagnostics;
using System.Text.Json;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.AuthContext.Options;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.Infrastructure.Caching;
using Matrix.ApiGateway.Infrastructure.Logging;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Authorization.AuthContext
{
    public sealed class CachedAuthContextStore(
        IDistributedCache distributedCache,
        IIdentityInternalUsersClient client,
        IOptions<AuthContextOptions> options,
        ILogger<CachedAuthContextStore> logger)
        : IAuthContextStore
    {
        private readonly AuthContextOptions _options = options.Value;

        public async Task<UserAuthContextResponse> GetAsync(
            Guid userId,
            int permissionsVersion,
            CancellationToken cancellationToken)
        {
            string cacheKey = AuthorizationCacheKeys.AuthContext(
                userId: userId,
                permissionsVersion: permissionsVersion);

            // 1) Try read from Redis
            try
            {
                var sw = Stopwatch.StartNew();
                string? json = await distributedCache.GetStringAsync(
                    key: cacheKey,
                    token: cancellationToken);
                sw.Stop();

                if (sw.ElapsedMilliseconds > CacheLoggingDefaults.SlowOperationMs &&
                    LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.AcRedisReadSlow,
                        period: CacheLoggingDefaults.SlowPeriod))
                    logger.LogWarning(
                        message: "Redis read is slow. CacheKey={CacheKey} UserId={UserId} ElapsedMs={ElapsedMs}",
                        cacheKey,
                        userId,
                        sw.ElapsedMilliseconds);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    UserAuthContextResponse? dto = JsonSerializer.Deserialize<UserAuthContextResponse>(json);
                    if (dto is not null &&
                        dto.EffectivePermissions is
                        {
                            Length: > 0
                        })
                    {
                        if (logger.IsEnabled(LogLevel.Debug))
                            logger.LogDebug(
                                message: "AuthContext cache hit for user {UserId}. Version={Version} Perms={Count}",
                                userId,
                                dto.PermissionsVersion,
                                dto.EffectivePermissions.Length);

                        return dto;
                    }

                    if (LogRateLimiter.ShouldLog(
                            key: RedisCacheLogKeys.AcRedisReadInvalid,
                            period: CacheLoggingDefaults.InvalidPeriod))
                        logger.LogWarning(
                            message:
                            "AuthContext cache value is invalid. CacheKey={CacheKey} UserId={UserId}. Falling back to Identity.",
                            cacheKey,
                            userId);
                }
                else
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug(
                            message:
                            "AuthContext cache miss for user {UserId}. CacheKey={CacheKey}. Falling back to Identity.",
                            userId,
                            cacheKey);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.AcRedisReadFail,
                        period: CacheLoggingDefaults.FailPeriod))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to read AuthContext cache (fallback to Identity). UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                        userId,
                        cacheKey,
                        ex.GetType()
                           .FullName);
            }

            // 2) Fallback to Identity
            UserAuthContextResponse ctx = await client.GetAuthContextAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            // 3) Try to write to Redis (the best effort)
            try
            {
                var sw = Stopwatch.StartNew();
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtlPolicy.GetTtlOrDefault(
                        ttlSeconds: _options.CacheTtlSeconds,
                        defaultTtlSeconds: 1800,
                        logKey: RedisCacheLogKeys.AcCacheTtlInvalid,
                        cacheName: "AuthContext",
                        logger: logger)
                };

                string json = JsonSerializer.Serialize(ctx);
                await distributedCache.SetStringAsync(
                    key: cacheKey,
                    value: json,
                    options: cacheOptions,
                    token: cancellationToken);
                sw.Stop();

                if (sw.ElapsedMilliseconds > CacheLoggingDefaults.SlowOperationMs &&
                    LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.AcRedisWriteSlow,
                        period: CacheLoggingDefaults.SlowPeriod))
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
                if (LogRateLimiter.ShouldLog(
                        key: RedisCacheLogKeys.AcRedisWriteFail,
                        period: CacheLoggingDefaults.FailPeriod))
                    logger.LogWarning(
                        exception: ex,
                        message:
                        "Failed to write AuthContext cache (best effort). UserId={UserId} CacheKey={CacheKey} ExceptionType={ExceptionType}",
                        userId,
                        cacheKey,
                        ex.GetType()
                           .FullName);
            }

            return ctx;
        }
    }
}
