namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions
{
    public interface IOutboxMessagePublisher
    {
        Task PublishAsync(
            Guid messageId,
            string type,
            string payloadJson,
            CancellationToken cancellationToken);
    }
}
