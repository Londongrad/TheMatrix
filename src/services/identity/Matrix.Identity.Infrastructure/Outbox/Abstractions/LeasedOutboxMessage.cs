namespace Matrix.Identity.Infrastructure.Outbox.Abstractions
{
    public sealed record LeasedOutboxMessage(
        Guid Id,
        string Type,
        string PayloadJson,
        int AttemptCount);
}
