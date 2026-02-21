namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface ISecurityAuditBulkRepository
    {
        Task<int> DeleteBatchAsync(
            DateTime occurredBeforeUtc,
            int batchSize,
            CancellationToken cancellationToken);
    }
}
