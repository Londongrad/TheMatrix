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
        IOptions<PermissionsVersionOptions> options)
        : IPermissionsVersionStore
    {
        private readonly PermissionsVersionOptions _options = options.Value;

        public async Task<int> GetCurrentAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            string cacheKey = PermissionsVersionCacheKeys.ForUser(userId);

            // 1) Try read from Redis
            try
            {
                string? cached = await distributedCache.GetStringAsync(
                    key: cacheKey,
                    token: cancellationToken);

                if (!string.IsNullOrWhiteSpace(cached) &&
                    int.TryParse(
                        s: cached,
                        style: NumberStyles.Integer,
                        provider: CultureInfo.InvariantCulture,
                        result: out int version))
                    return version;
            }
            catch
            {
                // Redis недоступен — просто идём дальше (fallback на Identity)
            }

            // 2) Fallback to Identity
            int currentVersion = await client.GetPermissionsVersionAsync(
                userId: userId,
                cancellationToken: cancellationToken);

            // 3) Try write to Redis (best effort)
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheTtlSeconds)
                };

                await distributedCache.SetStringAsync(
                    key: cacheKey,
                    value: currentVersion.ToString(CultureInfo.InvariantCulture),
                    options: cacheOptions,
                    token: cancellationToken);
            }
            catch
            {
                // Best effort: если Redis лег — не мешаем запросу
            }

            return currentVersion;
        }
    }
}
