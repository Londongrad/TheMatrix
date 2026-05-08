using Matrix.Identity.Infrastructure.Security.Tokens.Cleanup;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Tokens.Cleanup;

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
            timeProvider: CreateTimeProvider(new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero)));

        (int revokedDeletedCount, int expiredDeletedCount) = await cleaner.DeleteBatchAsync(
            new RefreshTokenCleanupOptions
            {
                BatchSize = 50,
                RevokedRetentionHours = 24,
                ExpiredRetentionHours = 12
            },
            CancellationToken.None);

        Assert.Equal(2, revokedDeletedCount);
        Assert.Equal(3, expiredDeletedCount);
        Assert.Equal(CreatedAtUtc.AddHours(-24), bulkRepository.LastRevokedBeforeUtc);
        Assert.Equal(CreatedAtUtc.AddHours(-12), bulkRepository.LastExpiredBeforeUtc);
        Assert.Equal(50, bulkRepository.LastBatchSize);
    }
}
