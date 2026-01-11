using System.Globalization;
using MassTransit;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.Identity.Contracts.Internal.Events;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Consumers
{
    public sealed class UserSecurityStateChangedConsumer(
        IDistributedCache cache,
        IOptions<PermissionsVersionOptions> options)
        : IConsumer<UserSecurityStateChangedV1>
    {
        public Task Consume(ConsumeContext<UserSecurityStateChangedV1> context)
        {
            UserSecurityStateChangedV1 msg = context.Message;

            string key = PermissionsVersionCacheKeys.ForUser(msg.UserId);

            return cache.SetStringAsync(
                key: key,
                value: msg.PermissionsVersion.ToString(CultureInfo.InvariantCulture),
                options: new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(options.Value.CacheTtlSeconds)
                },
                token: context.CancellationToken);
        }
    }
}
