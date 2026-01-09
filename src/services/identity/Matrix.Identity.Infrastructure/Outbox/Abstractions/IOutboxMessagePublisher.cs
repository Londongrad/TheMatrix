namespace Matrix.Identity.Infrastructure.Outbox.Abstractions
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
