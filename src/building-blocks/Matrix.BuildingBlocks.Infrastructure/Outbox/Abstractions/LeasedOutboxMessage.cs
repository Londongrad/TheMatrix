namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions
{
    public sealed record LeasedOutboxMessage(
        Guid Id,
        string Type,
        string PayloadJson,
        int AttemptCount);
}
