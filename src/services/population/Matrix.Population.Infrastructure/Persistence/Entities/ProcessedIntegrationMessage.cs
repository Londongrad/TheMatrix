namespace Matrix.Population.Infrastructure.Persistence.Entities
{
    public sealed class ProcessedIntegrationMessage
    {
        private ProcessedIntegrationMessage() { }

        public ProcessedIntegrationMessage(
            string consumer,
            Guid messageId,
            DateTimeOffset processedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(consumer))
                throw new ArgumentException("Consumer is required.", nameof(consumer));

            if (messageId == Guid.Empty)
                throw new ArgumentException("MessageId cannot be empty.", nameof(messageId));

            if (processedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("ProcessedAtUtc must be UTC.", nameof(processedAtUtc));

            Consumer = consumer;
            MessageId = messageId;
            ProcessedAtUtc = processedAtUtc;
        }

        public string Consumer { get; private set; } = null!;
        public Guid MessageId { get; private set; }
        public DateTimeOffset ProcessedAtUtc { get; private set; }
    }
}
