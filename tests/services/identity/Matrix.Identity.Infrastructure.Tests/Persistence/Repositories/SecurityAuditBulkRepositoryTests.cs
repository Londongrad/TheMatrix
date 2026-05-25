using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class SecurityAuditBulkRepositoryTests
    {
        [Fact]
        public async Task DeleteBatchAsync_UsesProviderAwareSqlAndDeletesOldestRowsFirst()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new SecurityAuditBulkRepository(database.DbContext);

            database.DbContext.SecurityAuditEvents.AddRange(
                CreateSecurityAuditRecord(
                    subject: "first",
                    occurredAtUtc: CreatedAtUtc),
                CreateSecurityAuditRecord(
                    subject: "second",
                    occurredAtUtc: CreatedAtUtc.AddMinutes(10)),
                CreateSecurityAuditRecord(
                    subject: "third",
                    occurredAtUtc: CreatedAtUtc.AddMinutes(20)));
            await database.DbContext.SaveChangesAsync();

            int deleted = await repository.DeleteBatchAsync(
                occurredBeforeUtc: CreatedAtUtc.AddMinutes(20),
                batchSize: 2,
                cancellationToken: CancellationToken.None);

            string[] subjects = await database.DbContext.SecurityAuditEvents
               .AsNoTracking()
               .OrderBy(x => x.OccurredAtUtc)
               .Select(x => x.Subject!)
               .ToArrayAsync();

            Assert.Equal(
                expected: 2,
                actual: deleted);
            Assert.Equal(
                expectedSpan: ["third"],
                actualArray: subjects);
        }
    }
}
