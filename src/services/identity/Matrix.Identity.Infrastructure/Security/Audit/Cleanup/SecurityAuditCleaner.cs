using Matrix.Identity.Application.Abstractions.Persistence;

namespace Matrix.Identity.Infrastructure.Security.Audit.Cleanup
{
    public sealed class SecurityAuditCleaner(
        ISecurityAuditBulkRepository securityAuditBulkRepository,
        TimeProvider timeProvider)
    {
        public async Task<int> DeleteBatchAsync(
            SecurityAuditCleanupOptions options,
            CancellationToken cancellationToken)
        {
            DateTime utcNow = timeProvider.GetUtcNow()
               .UtcDateTime;
            DateTime occurredBeforeUtc = options.RetentionDays <= 0
                ? utcNow
                : utcNow.AddDays(-options.RetentionDays);

            return await securityAuditBulkRepository.DeleteBatchAsync(
                occurredBeforeUtc: occurredBeforeUtc,
                batchSize: options.BatchSize,
                cancellationToken: cancellationToken);
        }
    }
}
