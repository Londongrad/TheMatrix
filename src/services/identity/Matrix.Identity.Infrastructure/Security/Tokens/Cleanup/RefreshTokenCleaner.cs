using Matrix.Identity.Application.Abstractions.Persistence;

namespace Matrix.Identity.Infrastructure.Security.Tokens.Cleanup
{
    public sealed class RefreshTokenCleaner(
        IRefreshTokenBulkRepository refreshTokenBulkRepository,
        TimeProvider timeProvider)
    {
        public async Task<(int revokedDeletedCount, int expiredDeletedCount)> DeleteBatchAsync(
            RefreshTokenCleanupOptions options,
            CancellationToken cancellationToken)
        {
            DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

            DateTime revokedBeforeUtc = options.RevokedRetentionHours <= 0
                ? utcNow
                : utcNow.AddHours(-options.RevokedRetentionHours);

            DateTime expiredBeforeUtc = options.ExpiredRetentionHours <= 0
                ? utcNow
                : utcNow.AddHours(-options.ExpiredRetentionHours);

            int revokedDeletedCount = await refreshTokenBulkRepository.DeleteRevokedBatchAsync(
                revokedBeforeUtc: revokedBeforeUtc,
                batchSize: options.BatchSize,
                cancellationToken: cancellationToken);

            int expiredDeletedCount = await refreshTokenBulkRepository.DeleteExpiredBatchAsync(
                expiredBeforeUtc: expiredBeforeUtc,
                batchSize: options.BatchSize,
                cancellationToken: cancellationToken);

            return (revokedDeletedCount, expiredDeletedCount);
        }
    }
}
