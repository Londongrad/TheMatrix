namespace Matrix.Population.Application.Abstractions
{
    public interface IProcessedIntegrationMessageRepository
    {
        Task<bool> TryMarkProcessedAsync(
            string consumer,
            Guid messageId,
            DateTimeOffset processedAtUtc,
            CancellationToken cancellationToken = default);
    }
}
