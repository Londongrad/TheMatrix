using Matrix.Identity.Infrastructure.Security.Audit.Cleanup;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Audit.Cleanup
{
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
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: CreatedAtUtc,
                        offset: TimeSpan.Zero)));

            int deletedCount = await cleaner.DeleteBatchAsync(
                options: new SecurityAuditCleanupOptions
                {
                    BatchSize = 25,
                    RetentionDays = 10
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 7,
                actual: deletedCount);
            Assert.Equal(
                expected: CreatedAtUtc.AddDays(-10),
                actual: bulkRepository.LastOccurredBeforeUtc);
            Assert.Equal(
                expected: 25,
                actual: bulkRepository.LastBatchSize);
        }
    }
}
