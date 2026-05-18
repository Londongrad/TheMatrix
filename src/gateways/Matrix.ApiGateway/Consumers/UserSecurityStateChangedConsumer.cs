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
    public sealed class UserSecurityStateChangedConsumer(
        IDistributedCache cache,
        IOptions<PermissionsVersionOptions> options,
        ILogger<UserSecurityStateChangedConsumer> logger)
        : IConsumer<UserSecurityStateChangedV1>
    {
        public Task Consume(ConsumeContext<UserSecurityStateChangedV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal Task ConsumeAsync(
            UserSecurityStateChangedV1 message,
            CancellationToken cancellationToken)
        {
            UserSecurityStateChangedV1 msg = message;

            string key = AuthorizationCacheKeys.PermissionsVersion(msg.UserId);

            TimeSpan ttl = CacheTtlPolicy.GetTtlOrDefault(
                ttlSeconds: options.Value.CacheTtlSeconds,
                defaultTtlSeconds: 1800,
                logKey: RedisCacheLogKeys.PvCacheTtlInvalid,
                cacheName: "PermissionsVersion",
                logger: logger);

            return cache.SetStringAsync(
                key: key,
                value: msg.PermissionsVersion.ToString(CultureInfo.InvariantCulture),
                options: new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                },
                token: cancellationToken);
        }
    }
}
