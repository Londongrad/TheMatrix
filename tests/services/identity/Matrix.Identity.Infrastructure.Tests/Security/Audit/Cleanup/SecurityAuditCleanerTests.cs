using Matrix.Identity.Infrastructure.Security.Audit.Cleanup;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Audit.Cleanup;

public sealed class SecurityAuditCleanerTests
{
    [Fact]
    public async Task DeleteBatchAsync_ComputesRetentionCutoffFromTimeProvider()
    {
        var bulkRepository = new FakeSecurityAuditBulkRepository
        {
            DeleteBatchResult = 7
        };
        var cleaner = new SecurityAuditCleaner(
            securityAuditBulkRepository: bulkRepository,
            timeProvider: CreateTimeProvider(new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero)));

        int deletedCount = await cleaner.DeleteBatchAsync(
            new SecurityAuditCleanupOptions
            {
                BatchSize = 25,
                RetentionDays = 10
            },
            CancellationToken.None);

        Assert.Equal(7, deletedCount);
        Assert.Equal(CreatedAtUtc.AddDays(-10), bulkRepository.LastOccurredBeforeUtc);
        Assert.Equal(25, bulkRepository.LastBatchSize);
    }
}
