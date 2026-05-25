using Matrix.Identity.Infrastructure.Security.Tokens.Cleanup;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens.Cleanup
{
    public sealed class RefreshTokenCleanerTests
    {
        [Fact]
        public async Task DeleteBatchAsync_ComputesRevokedAndExpiredCutoffsFromTimeProvider()
        {
            var bulkRepository = new FakeRefreshTokenBulkRepository
            {
                DeleteRevokedBatchResult = 2,
                DeleteExpiredBatchResult = 3
            };
            var cleaner = new RefreshTokenCleaner(
                refreshTokenBulkRepository: bulkRepository,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));

            (int revokedDeletedCount, int expiredDeletedCount) = await cleaner.DeleteBatchAsync(
                options: new RefreshTokenCleanupOptions
                {
                    BatchSize = 50,
                    RevokedRetentionHours = 24,
                    ExpiredRetentionHours = 12
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: revokedDeletedCount);
            Assert.Equal(
                expected: 3,
                actual: expiredDeletedCount);
            Assert.Equal(
                expected: CreatedAtUtc.AddHours(-24),
                actual: bulkRepository.LastRevokedBeforeUtc);
            Assert.Equal(
                expected: CreatedAtUtc.AddHours(-12),
                actual: bulkRepository.LastExpiredBeforeUtc);
            Assert.Equal(
                expected: 50,
                actual: bulkRepository.LastBatchSize);
        }
    }
}
