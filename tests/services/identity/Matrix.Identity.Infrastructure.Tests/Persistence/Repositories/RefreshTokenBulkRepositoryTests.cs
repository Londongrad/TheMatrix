using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories;

public sealed class RefreshTokenBulkRepositoryTests
{
    [Fact]
    public async Task RevokeAllByUserIdAsync_RevokesOnlyActiveTokensForUserUsingClockTimestamp()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        DateTime nowUtc = LaterUtc;
        var repository = new RefreshTokenBulkRepository(database.DbContext, CreateClock(nowUtc));
        User targetUser = CreateUser("target@matrix.local", "target");
        User otherUser = CreateUser("other@matrix.local", "other");
        RefreshToken active = IssueRefreshToken(targetUser, tokenHash: "active", expiresAtUtc: nowUtc.AddHours(4));
        RefreshToken alreadyRevoked = IssueRefreshToken(targetUser, tokenHash: "revoked", expiresAtUtc: nowUtc.AddHours(4));
        RefreshToken otherUserToken = IssueRefreshToken(otherUser, tokenHash: "other-active", expiresAtUtc: nowUtc.AddHours(4));
        alreadyRevoked.Revoke(RefreshTokenRevocationReason.AdminRevoked, CreatedAtUtc.AddMinutes(10));

        await database.DbContext.Users.AddRangeAsync(targetUser, otherUser);
        await database.DbContext.SaveChangesAsync();

        int affected = await repository.RevokeAllByUserIdAsync(
            targetUser.Id,
            RefreshTokenRevocationReason.UserRevoked,
            CancellationToken.None);

        RefreshToken[] tokens = await database.DbContext.Users
            .SelectMany(x => x.RefreshTokens)
            .AsNoTracking()
            .OrderBy(x => x.TokenHash)
            .ToArrayAsync();

        Assert.Equal(1, affected);
        Assert.True(tokens.Single(x => x.TokenHash == "active").IsRevoked);
        Assert.Equal(nowUtc, tokens.Single(x => x.TokenHash == "active").RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.UserRevoked, tokens.Single(x => x.TokenHash == "active").RevokedReason);
        Assert.Equal(RefreshTokenRevocationReason.AdminRevoked, tokens.Single(x => x.TokenHash == "revoked").RevokedReason);
        Assert.False(tokens.Single(x => x.TokenHash == "other-active").IsRevoked);
    }

    [Fact]
    public async Task RevokeByIdAsync_RevokesOnlyMatchingToken()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        DateTime nowUtc = LaterUtc;
        var repository = new RefreshTokenBulkRepository(database.DbContext, CreateClock(nowUtc));
        User user = CreateUser();
        RefreshToken first = IssueRefreshToken(user, tokenHash: "first", expiresAtUtc: nowUtc.AddHours(4));
        RefreshToken second = IssueRefreshToken(user, tokenHash: "second", expiresAtUtc: nowUtc.AddHours(4));

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        int affected = await repository.RevokeByIdAsync(
            user.Id,
            first.Id,
            RefreshTokenRevocationReason.SessionReplaced,
            CancellationToken.None);

        RefreshToken[] tokens = await database.DbContext.Users
            .SelectMany(x => x.RefreshTokens)
            .AsNoTracking()
            .OrderBy(x => x.TokenHash)
            .ToArrayAsync();

        Assert.Equal(1, affected);
        Assert.True(tokens.Single(x => x.TokenHash == "first").IsRevoked);
        Assert.False(tokens.Single(x => x.TokenHash == "second").IsRevoked);
    }

    [Fact]
    public async Task DeleteExpiredAsync_AndDeleteRevokedAndExpiredAsync_RemoveExpectedRows()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new RefreshTokenBulkRepository(database.DbContext, CreateClock(LaterUtc));
        User user = CreateUser();
        DateTime nowUtc = LaterUtc;
        RefreshToken expiredActive = IssueRefreshToken(user, tokenHash: "expired-active", createdAtUtc: CreatedAtUtc, expiresAtUtc: nowUtc.AddMinutes(-10));
        RefreshToken expiredRevoked = IssueRefreshToken(user, tokenHash: "expired-revoked", createdAtUtc: CreatedAtUtc, expiresAtUtc: nowUtc.AddMinutes(-5));
        RefreshToken active = IssueRefreshToken(user, tokenHash: "active", createdAtUtc: CreatedAtUtc, expiresAtUtc: nowUtc.AddHours(2));
        expiredRevoked.Revoke(RefreshTokenRevocationReason.UserRevoked, CreatedAtUtc.AddMinutes(20));

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        int deletedExpired = await repository.DeleteExpiredAsync(nowUtc, CancellationToken.None);
        string[] afterExpiredDelete = await database.DbContext.Users
            .SelectMany(x => x.RefreshTokens)
            .AsNoTracking()
            .Select(x => x.TokenHash)
            .OrderBy(x => x)
            .ToArrayAsync();

        Assert.Equal(2, deletedExpired);
        Assert.Equal(["active"], afterExpiredDelete);

        RefreshToken expiredAndRevokedAgain = IssueRefreshToken(user, tokenHash: "expired-revoked-2", createdAtUtc: CreatedAtUtc, expiresAtUtc: nowUtc.AddMinutes(-1));
        expiredAndRevokedAgain.Revoke(RefreshTokenRevocationReason.UserRevoked, CreatedAtUtc.AddMinutes(30));
        await database.DbContext.SaveChangesAsync();

        int deletedRevokedExpired = await repository.DeleteRevokedAndExpiredAsync(nowUtc, CancellationToken.None);
        string[] remaining = await database.DbContext.Users
            .SelectMany(x => x.RefreshTokens)
            .AsNoTracking()
            .Select(x => x.TokenHash)
            .OrderBy(x => x)
            .ToArrayAsync();

        Assert.Equal(1, deletedRevokedExpired);
        Assert.Equal(["active"], remaining);
    }

    [Fact]
    public async Task BatchDeleteMethods_UseProviderAwareSqlAndDeleteOldestRowsFirst()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new RefreshTokenBulkRepository(database.DbContext, CreateClock(LaterUtc));
        User user = CreateUser();
        RefreshToken expiredOldest = IssueRefreshToken(user, tokenHash: "expired-1", createdAtUtc: CreatedAtUtc, expiresAtUtc: CreatedAtUtc.AddMinutes(10));
        RefreshToken expiredNext = IssueRefreshToken(user, tokenHash: "expired-2", createdAtUtc: CreatedAtUtc, expiresAtUtc: CreatedAtUtc.AddMinutes(20));
        RefreshToken expiredNewest = IssueRefreshToken(user, tokenHash: "expired-3", createdAtUtc: CreatedAtUtc, expiresAtUtc: CreatedAtUtc.AddMinutes(30));
        RefreshToken revokedOldest = IssueRefreshToken(user, tokenHash: "revoked-1", createdAtUtc: CreatedAtUtc, expiresAtUtc: LaterUtc.AddHours(2));
        RefreshToken revokedNext = IssueRefreshToken(user, tokenHash: "revoked-2", createdAtUtc: CreatedAtUtc, expiresAtUtc: LaterUtc.AddHours(2));
        RefreshToken active = IssueRefreshToken(user, tokenHash: "active", createdAtUtc: CreatedAtUtc, expiresAtUtc: LaterUtc.AddHours(2));
        revokedOldest.Revoke(RefreshTokenRevocationReason.AdminRevoked, CreatedAtUtc.AddMinutes(5));
        revokedNext.Revoke(RefreshTokenRevocationReason.AdminRevoked, CreatedAtUtc.AddMinutes(15));

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        int expiredDeleted = await repository.DeleteExpiredBatchAsync(CreatedAtUtc.AddMinutes(40), 2, CancellationToken.None);
        string[] afterExpired = await database.DbContext.Users
            .SelectMany(x => x.RefreshTokens)
            .AsNoTracking()
            .Select(x => x.TokenHash)
            .OrderBy(x => x)
            .ToArrayAsync();

        Assert.Equal(2, expiredDeleted);
        Assert.DoesNotContain("expired-1", afterExpired);
        Assert.DoesNotContain("expired-2", afterExpired);
        Assert.Contains("expired-3", afterExpired);

        int revokedDeleted = await repository.DeleteRevokedBatchAsync(CreatedAtUtc.AddMinutes(20), 1, CancellationToken.None);
        string[] afterRevoked = await database.DbContext.Users
            .SelectMany(x => x.RefreshTokens)
            .AsNoTracking()
            .Select(x => x.TokenHash)
            .OrderBy(x => x)
            .ToArrayAsync();

        Assert.Equal(1, revokedDeleted);
        Assert.DoesNotContain("revoked-1", afterRevoked);
        Assert.Contains("revoked-2", afterRevoked);
        Assert.Contains("active", afterRevoked);
    }
}
