using System.Globalization;
using MassTransit;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.Infrastructure.Caching;
using Matrix.Identity.Contracts.Internal.Events;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Consumers
{
    public sealed class DefaultUserAccessPolicyChangedConsumer(
        IDistributedCache cache,
        IOptions<PermissionsVersionOptions> options,
        ILogger<DefaultUserAccessPolicyChangedConsumer> logger)
        : IConsumer<DefaultUserAccessPolicyChangedV1>
    {
        public async Task Consume(ConsumeContext<DefaultUserAccessPolicyChangedV1> context)
        {
            await ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            DefaultUserAccessPolicyChangedV1 message,
            CancellationToken cancellationToken)
        {
            string key = AuthorizationCacheKeys.DefaultUserAccessVersion();
            string staleKey = AuthorizationCacheKeys.DefaultUserAccessVersionStale();

            TimeSpan freshTtl = CacheTtlPolicy.GetTtlOrDefault(
                ttlSeconds: options.Value.CacheTtlSeconds,
                defaultTtlSeconds: 1800,
                logKey: RedisCacheLogKeys.PvCacheTtlInvalid,
                cacheName: "DefaultUserAccessVersion",
                logger: logger);

            TimeSpan staleTtl = CacheTtlPolicy.GetTtlOrDefault(
                ttlSeconds: options.Value.StaleCacheTtlSeconds,
                defaultTtlSeconds: 1800,
                logKey: RedisCacheLogKeys.PvStaleCacheTtlInvalid,
                cacheName: "DefaultUserAccessVersionStale",
                logger: logger);

            string value = message.Version.ToString(CultureInfo.InvariantCulture);

            await cache.SetStringAsync(
                key: key,
                value: value,
                options: new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = freshTtl
                },
                token: cancellationToken);

            await cache.SetStringAsync(
                key: staleKey,
                value: value,
                options: new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = staleTtl
                },
                token: cancellationToken);
        }
    }
}
