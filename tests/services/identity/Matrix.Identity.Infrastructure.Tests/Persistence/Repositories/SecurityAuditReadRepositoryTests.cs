using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories;

public sealed class SecurityAuditReadRepositoryTests
{
    [Fact]
    public async Task GetSliceByUserIdAsync_ReturnsNormalizedPageAndNextCursor()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        User user = CreateUser();
        await database.DbContext.Users.AddAsync(user);
        database.DbContext.SecurityAuditEvents.AddRange(
            CreateSecurityAuditRecord(userId: user.Id, eventType: SecurityAuditEventType.Login, subject: "oldest", occurredAtUtc: CreatedAtUtc),
            CreateSecurityAuditRecord(userId: user.Id, eventType: SecurityAuditEventType.PasswordResetRequested, subject: "middle", occurredAtUtc: CreatedAtUtc.AddMinutes(10)),
            CreateSecurityAuditRecord(userId: user.Id, eventType: SecurityAuditEventType.EmailConfirmed, isSuccessful: true, subject: "newest", occurredAtUtc: CreatedAtUtc.AddMinutes(20)));
        await database.DbContext.SaveChangesAsync();

        var repository = new SecurityAuditReadRepository(
            database.DbContext,
            new TestLogger<SecurityAuditReadRepository>());

        var slice = await repository.GetSliceByUserIdAsync(user.Id, null, 2, CancellationToken.None);

        SecurityActivityItemResult[] items = slice.Items.ToArray();
        Assert.Equal(2, slice.PageSize);
        Assert.True(slice.HasNext);
        Assert.Equal(
            [SecurityAuditEventType.EmailConfirmed, SecurityAuditEventType.PasswordResetRequested],
            items.Select(x => x.EventType).ToArray());
        Assert.NotNull(slice.NextCursor);
    }

    [Fact]
    public async Task GetSliceByUserIdAsync_WhenCursorProvided_ReturnsOlderItemsOnly()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        User user = CreateUser();
        await database.DbContext.Users.AddAsync(user);
        var oldest = CreateSecurityAuditRecord(userId: user.Id, subject: "oldest", occurredAtUtc: CreatedAtUtc);
        var middle = CreateSecurityAuditRecord(userId: user.Id, subject: "middle", occurredAtUtc: CreatedAtUtc.AddMinutes(10));
        var newest = CreateSecurityAuditRecord(userId: user.Id, subject: "newest", occurredAtUtc: CreatedAtUtc.AddMinutes(20));
        database.DbContext.SecurityAuditEvents.AddRange(oldest, middle, newest);
        await database.DbContext.SaveChangesAsync();

        var repository = new SecurityAuditReadRepository(
            database.DbContext,
            new TestLogger<SecurityAuditReadRepository>());
        string firstCursor = SecurityActivityCursorCodec.Encode(new SecurityActivityCursor(middle.OccurredAtUtc.Ticks, middle.Id));

        var slice = await repository.GetSliceByUserIdAsync(
            user.Id,
            SecurityActivityCursorCodec.TryDecode(firstCursor, out SecurityActivityCursor cursor) ? cursor : null,
            10,
            CancellationToken.None);

        SecurityActivityItemResult item = Assert.Single(slice.Items);
        Assert.Equal(oldest.Id, item.EventId);
        Assert.False(slice.HasNext);
    }

    [Fact]
    public async Task GetSliceByUserIdAsync_NormalizesRequestedPageSize()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        User user = CreateUser();
        await database.DbContext.Users.AddAsync(user);
        database.DbContext.SecurityAuditEvents.Add(CreateSecurityAuditRecord(userId: user.Id, occurredAtUtc: CreatedAtUtc));
        await database.DbContext.SaveChangesAsync();

        var repository = new SecurityAuditReadRepository(
            database.DbContext,
            new TestLogger<SecurityAuditReadRepository>());

        var slice = await repository.GetSliceByUserIdAsync(user.Id, null, 0, CancellationToken.None);

        Assert.Equal(SecurityActivityPageSizePolicy.DefaultPageSize, slice.PageSize);
    }
}
