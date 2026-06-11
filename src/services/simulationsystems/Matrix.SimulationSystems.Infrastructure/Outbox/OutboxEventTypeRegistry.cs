namespace Matrix.SimulationSystems.Infrastructure.Outbox
{
    public sealed class OutboxEventTypeRegistry
    {
        private readonly IReadOnlyDictionary<string, Type> _eventTypes;

        public OutboxEventTypeRegistry(IEnumerable<IOutboxEventTypeContributor> contributors)
        {
            var eventTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (IOutboxEventTypeContributor contributor in contributors)
            {
                foreach ((string eventType, Type clrType) in contributor.EventTypes)
                {
                    if (string.IsNullOrWhiteSpace(eventType))
                        throw new InvalidOperationException("An outbox event type key cannot be empty.");

                    if (!eventTypes.TryAdd(eventType, clrType))
                        throw new InvalidOperationException(
                            $"Outbox event type '{eventType}' has more than one CLR type registration.");
                }
            }

            _eventTypes = eventTypes;
        }

        public int Count => _eventTypes.Count;

        public Type Resolve(string eventType)
        {
            return _eventTypes.TryGetValue(eventType, out Type? clrType)
                ? clrType
                : throw new NotSupportedException($"Outbox message type '{eventType}' is not supported.");
        }
    }
}
