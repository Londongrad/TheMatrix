using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class SecurityAuditReadRepositoryTests
    {
        [Fact]
        public async Task GetSliceByUserIdAsync_ReturnsNormalizedPageAndNextCursor()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            User user = CreateUser();
            await database.DbContext.Users.AddAsync(user);
            database.DbContext.SecurityAuditEvents.AddRange(
                CreateSecurityAuditRecord(
                    userId: user.Id,
                    eventType: SecurityAuditEventType.Login,
                    subject: "oldest",
                    occurredAtUtc: CreatedAtUtc),
                CreateSecurityAuditRecord(
                    userId: user.Id,
                    eventType: SecurityAuditEventType.PasswordResetRequested,
                    subject: "middle",
                    occurredAtUtc: CreatedAtUtc.AddMinutes(10)),
                CreateSecurityAuditRecord(
                    userId: user.Id,
                    eventType: SecurityAuditEventType.EmailConfirmed,
                    isSuccessful: true,
                    subject: "newest",
                    occurredAtUtc: CreatedAtUtc.AddMinutes(20)));
            await database.DbContext.SaveChangesAsync();

            var repository = new SecurityAuditReadRepository(
                dbContext: database.DbContext,
                logger: new TestLogger<SecurityAuditReadRepository>());

            CursorPagedResult<SecurityActivityItemResult> slice = await repository.GetSliceByUserIdAsync(
                userId: user.Id,
                cursor: null,
                pageSize: 2,
                cancellationToken: CancellationToken.None);

            SecurityActivityItemResult[] items = slice.Items.ToArray();
            Assert.Equal(
                expected: 2,
                actual: slice.PageSize);
            Assert.True(slice.HasNext);
            Assert.Equal(
                expected:
                [
                    SecurityAuditEventType.EmailConfirmed,
                    SecurityAuditEventType.PasswordResetRequested
                ],
                actual: items.Select(x => x.EventType)
                   .ToArray());
            Assert.NotNull(slice.NextCursor);
        }

        [Fact]
        public async Task GetSliceByUserIdAsync_WhenCursorProvided_ReturnsOlderItemsOnly()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            User user = CreateUser();
            await database.DbContext.Users.AddAsync(user);
            SecurityAuditEventRecord oldest = CreateSecurityAuditRecord(
                userId: user.Id,
                subject: "oldest",
                occurredAtUtc: CreatedAtUtc);
            SecurityAuditEventRecord middle = CreateSecurityAuditRecord(
                userId: user.Id,
                subject: "middle",
                occurredAtUtc: CreatedAtUtc.AddMinutes(10));
            SecurityAuditEventRecord newest = CreateSecurityAuditRecord(
                userId: user.Id,
                subject: "newest",
                occurredAtUtc: CreatedAtUtc.AddMinutes(20));
            database.DbContext.SecurityAuditEvents.AddRange(
                oldest,
                middle,
                newest);
            await database.DbContext.SaveChangesAsync();

            var repository = new SecurityAuditReadRepository(
                dbContext: database.DbContext,
                logger: new TestLogger<SecurityAuditReadRepository>());
            string firstCursor = SecurityActivityCursorCodec.Encode(
                new SecurityActivityCursor(
                    UtcTicks: middle.OccurredAtUtc.Ticks,
                    EventId: middle.Id));

            CursorPagedResult<SecurityActivityItemResult> slice = await repository.GetSliceByUserIdAsync(
                userId: user.Id,
                cursor: SecurityActivityCursorCodec.TryDecode(
                    rawCursor: firstCursor,
                    cursor: out SecurityActivityCursor cursor)
                    ? cursor
                    : null,
                pageSize: 10,
                cancellationToken: CancellationToken.None);

            SecurityActivityItemResult item = Assert.Single(slice.Items);
            Assert.Equal(
                expected: oldest.Id,
                actual: item.EventId);
            Assert.False(slice.HasNext);
        }

        [Fact]
        public async Task GetSliceByUserIdAsync_NormalizesRequestedPageSize()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            User user = CreateUser();
            await database.DbContext.Users.AddAsync(user);
            database.DbContext.SecurityAuditEvents.Add(
                CreateSecurityAuditRecord(
                    userId: user.Id,
                    occurredAtUtc: CreatedAtUtc));
            await database.DbContext.SaveChangesAsync();

            var repository = new SecurityAuditReadRepository(
                dbContext: database.DbContext,
                logger: new TestLogger<SecurityAuditReadRepository>());

            CursorPagedResult<SecurityActivityItemResult> slice = await repository.GetSliceByUserIdAsync(
                userId: user.Id,
                cursor: null,
                pageSize: 0,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SecurityActivityPageSizePolicy.DefaultPageSize,
                actual: slice.PageSize);
        }
    }
}
