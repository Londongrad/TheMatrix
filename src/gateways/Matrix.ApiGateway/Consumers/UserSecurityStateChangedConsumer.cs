using System.Globalization;
using MassTransit;
using Matrix.Identity.Contracts.Internal.Events;
using Microsoft.Extensions.Caching.Distributed;

namespace Matrix.ApiGateway.Consumers
{
    public sealed class UserSecurityStateChangedConsumer(IDistributedCache cache)
        : IConsumer<UserSecurityStateChangedV1>
    {
        public Task Consume(ConsumeContext<UserSecurityStateChangedV1> context)
        {
            UserSecurityStateChangedV1 msg = context.Message;

            string key = $"pv:{msg.UserId}";

            return cache.SetStringAsync(
                key: key,
                value: msg.PermissionsVersion.ToString(CultureInfo.InvariantCulture),
                options: new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                },
                token: context.CancellationToken);
        }
    }
}
