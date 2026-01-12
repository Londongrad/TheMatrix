using System.Globalization;
using MassTransit;
using Matrix.ApiGateway.Authorization;
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
            UserSecurityStateChangedV1 msg = context.Message;

            string key = AuthorizationCacheKeys.PermissionsVersion(msg.UserId);

            TimeSpan ttl = CacheTtlPolicy.GetTtlOrDefault(
                ttlSeconds: options.Value.CacheTtlSeconds,
                defaultTtlSeconds: 1800,
                logKey: "ac.cache.ttl.invalid",
                cacheName: "AuthContext",
                logger: logger);

            return cache.SetStringAsync(
                key: key,
                value: msg.PermissionsVersion.ToString(CultureInfo.InvariantCulture),
                options: new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                },
                token: context.CancellationToken);
        }
    }
}
