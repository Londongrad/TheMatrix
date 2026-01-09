using Matrix.Identity.Contracts.Internal.Events;

namespace Matrix.Identity.Infrastructure.Outbox
{
    public static class OutboxEventTypeMap
    {
        public static readonly IReadOnlyDictionary<string, Type> Map =
            new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [InternalEventTypes.UserSecurityStateChangedV1] = typeof(UserSecurityStateChangedV1)
            };
    }
}
