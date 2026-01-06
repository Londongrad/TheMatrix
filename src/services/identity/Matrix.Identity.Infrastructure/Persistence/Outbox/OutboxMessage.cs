using System.Text.Json;

namespace Matrix.Identity.Infrastructure.Persistence.Outbox
{
    /// <summary>
    ///     Represents a message stored in the outbox for reliable publishing.
    /// </summary>
    public sealed class OutboxMessage
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web);

        private OutboxMessage() { }

        public Guid Id { get; private set; }
        public DateTime OccurredOnUtc { get; private set; }
        public string Type { get; private set; } = string.Empty;
        public string PayloadJson { get; private set; } = string.Empty;

        public DateTime? ProcessedOnUtc { get; private set; }
        public string? Error { get; private set; }

        // Multi-instance / retry
        public Guid? LockToken { get; private set; }
        public DateTime? LockedUntilUtc { get; private set; }
        public int AttemptCount { get; private set; }
        public DateTime? NextAttemptOnUtc { get; private set; }
        public DateTime? LastAttemptOnUtc { get; private set; }

        public static OutboxMessage Create(
            string type,
            DateTime occurredOnUtc,
            object payload,
            JsonSerializerOptions? jsonOptions = null)
        {
            return new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = occurredOnUtc,
                Type = type,
                PayloadJson = JsonSerializer.Serialize(
                    value: payload,
                    options: jsonOptions ?? DefaultJsonOptions),
                AttemptCount = 0
            };
        }

        public void MarkProcessed(DateTime processedOnUtc)
        {
            ProcessedOnUtc = processedOnUtc;
            Error = null;

            LockToken = null;
            LockedUntilUtc = null;
            NextAttemptOnUtc = null;
        }

        public void MarkFailed(
            string error,
            DateTime nextAttemptOnUtc)
        {
            Error = error;
            NextAttemptOnUtc = nextAttemptOnUtc;

            LockToken = null;
            LockedUntilUtc = null;
        }
    }
}
