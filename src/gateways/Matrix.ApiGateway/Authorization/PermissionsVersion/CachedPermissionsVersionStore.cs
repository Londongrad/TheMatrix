using System.Globalization;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
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

            int currentVersion = await client.GetPermissionsVersionAsync(
                userId,
                cancellationToken);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.CacheTtlSeconds)
            };

            await distributedCache.SetStringAsync(
                key: cacheKey,
                value: currentVersion.ToString(CultureInfo.InvariantCulture),
                options: cacheOptions,
                token: cancellationToken);

            return currentVersion;
        }
    }
}
