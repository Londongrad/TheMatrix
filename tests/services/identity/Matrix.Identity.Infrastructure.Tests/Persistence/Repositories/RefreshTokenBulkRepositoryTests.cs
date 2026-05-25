using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class RefreshTokenBulkRepositoryTests
    {
        [Fact]
        public async Task RevokeAllByUserIdAsync_RevokesOnlyActiveTokensForUserUsingClockTimestamp()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            DateTime nowUtc = LaterUtc;
            var repository = new RefreshTokenBulkRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: nowUtc,
                        offset: TimeSpan.Zero)));
            User targetUser = CreateUser(
                email: "target@matrix.local",
                username: "target");
            User otherUser = CreateUser(
                email: "other@matrix.local",
                username: "other");
            RefreshToken active = IssueRefreshToken(
                user: targetUser,
                tokenHash: "active",
                expiresAtUtc: nowUtc.AddHours(4));
            RefreshToken alreadyRevoked = IssueRefreshToken(
                user: targetUser,
                tokenHash: "revoked",
                expiresAtUtc: nowUtc.AddHours(4));
            RefreshToken otherUserToken = IssueRefreshToken(
                user: otherUser,
                tokenHash: "other-active",
                expiresAtUtc: nowUtc.AddHours(4));
            alreadyRevoked.Revoke(
                reason: RefreshTokenRevocationReason.AdminRevoked,
                revokedAtUtc: CreatedAtUtc.AddMinutes(10));

            await database.DbContext.Users.AddRangeAsync(
                targetUser,
                otherUser);
            await database.DbContext.SaveChangesAsync();

            int affected = await repository.RevokeAllByUserIdAsync(
                userId: targetUser.Id,
                reason: RefreshTokenRevocationReason.UserRevoked,
                cancellationToken: CancellationToken.None);

            RefreshToken[] tokens = await database.DbContext.Users
               .SelectMany(x => x.RefreshTokens)
               .AsNoTracking()
               .OrderBy(x => x.TokenHash)
               .ToArrayAsync();

            Assert.Equal(
                expected: 1,
                actual: affected);
            Assert.True(
                tokens.Single(x => x.TokenHash == "active")
                   .IsRevoked);
            Assert.Equal(
                expected: nowUtc,
                actual: tokens.Single(x => x.TokenHash == "active")
                   .RevokedAtUtc);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.UserRevoked,
                actual: tokens.Single(x => x.TokenHash == "active")
                   .RevokedReason);
            Assert.Equal(
                expected: RefreshTokenRevocationReason.AdminRevoked,
                actual: tokens.Single(x => x.TokenHash == "revoked")
                   .RevokedReason);
            Assert.False(
                tokens.Single(x => x.TokenHash == "other-active")
                   .IsRevoked);
        }

        [Fact]
        public async Task RevokeByIdAsync_RevokesOnlyMatchingToken()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            DateTime nowUtc = LaterUtc;
            var repository = new RefreshTokenBulkRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: nowUtc,
                        offset: TimeSpan.Zero)));
            User user = CreateUser();
            RefreshToken first = IssueRefreshToken(
                user: user,
                tokenHash: "first",
                expiresAtUtc: nowUtc.AddHours(4));
            RefreshToken second = IssueRefreshToken(
                user: user,
                tokenHash: "second",
                expiresAtUtc: nowUtc.AddHours(4));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.SaveChangesAsync();

            int affected = await repository.RevokeByIdAsync(
                userId: user.Id,
                refreshTokenId: first.Id,
                reason: RefreshTokenRevocationReason.SessionReplaced,
                cancellationToken: CancellationToken.None);

            RefreshToken[] tokens = await database.DbContext.Users
               .SelectMany(x => x.RefreshTokens)
               .AsNoTracking()
               .OrderBy(x => x.TokenHash)
               .ToArrayAsync();

            Assert.Equal(
                expected: 1,
                actual: affected);
            Assert.True(
                tokens.Single(x => x.TokenHash == "first")
                   .IsRevoked);
            Assert.False(
                tokens.Single(x => x.TokenHash == "second")
                   .IsRevoked);
        }

        [Fact]
        public async Task DeleteExpiredAsync_AndDeleteRevokedAndExpiredAsync_RemoveExpectedRows()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new RefreshTokenBulkRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)));
            User user = CreateUser();
            DateTime nowUtc = LaterUtc;
            RefreshToken expiredActive = IssueRefreshToken(
                user: user,
                tokenHash: "expired-active",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: nowUtc.AddMinutes(-10));
            RefreshToken expiredRevoked = IssueRefreshToken(
                user: user,
                tokenHash: "expired-revoked",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: nowUtc.AddMinutes(-5));
            RefreshToken active = IssueRefreshToken(
                user: user,
                tokenHash: "active",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: nowUtc.AddHours(2));
            expiredRevoked.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: CreatedAtUtc.AddMinutes(20));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.SaveChangesAsync();

            int deletedExpired = await repository.DeleteExpiredAsync(
                utcNow: nowUtc,
                cancellationToken: CancellationToken.None);
            string[] afterExpiredDelete = await database.DbContext.Users
               .SelectMany(x => x.RefreshTokens)
               .AsNoTracking()
               .Select(x => x.TokenHash)
               .OrderBy(x => x)
               .ToArrayAsync();

            Assert.Equal(
                expected: 2,
                actual: deletedExpired);
            Assert.Equal(
                expectedSpan: ["active"],
                actualArray: afterExpiredDelete);

            RefreshToken expiredAndRevokedAgain = IssueRefreshToken(
                user: user,
                tokenHash: "expired-revoked-2",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: nowUtc.AddMinutes(-1));
            expiredAndRevokedAgain.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: CreatedAtUtc.AddMinutes(30));
            await database.DbContext.SaveChangesAsync();

            int deletedRevokedExpired = await repository.DeleteRevokedAndExpiredAsync(
                utcNow: nowUtc,
                cancellationToken: CancellationToken.None);
            string[] remaining = await database.DbContext.Users
               .SelectMany(x => x.RefreshTokens)
               .AsNoTracking()
               .Select(x => x.TokenHash)
               .OrderBy(x => x)
               .ToArrayAsync();

            Assert.Equal(
                expected: 1,
                actual: deletedRevokedExpired);
            Assert.Equal(
                expectedSpan: ["active"],
                actualArray: remaining);
        }

        [Fact]
        public async Task BatchDeleteMethods_UseProviderAwareSqlAndDeleteOldestRowsFirst()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new RefreshTokenBulkRepository(
                db: database.DbContext,
                timeProvider: CreateTimeProvider(
                    new DateTimeOffset(
                        dateTime: LaterUtc,
                        offset: TimeSpan.Zero)));
            User user = CreateUser();
            RefreshToken expiredOldest = IssueRefreshToken(
                user: user,
                tokenHash: "expired-1",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddMinutes(10));
            RefreshToken expiredNext = IssueRefreshToken(
                user: user,
                tokenHash: "expired-2",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddMinutes(20));
            RefreshToken expiredNewest = IssueRefreshToken(
                user: user,
                tokenHash: "expired-3",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddMinutes(30));
            RefreshToken revokedOldest = IssueRefreshToken(
                user: user,
                tokenHash: "revoked-1",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: LaterUtc.AddHours(2));
            RefreshToken revokedNext = IssueRefreshToken(
                user: user,
                tokenHash: "revoked-2",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: LaterUtc.AddHours(2));
            RefreshToken active = IssueRefreshToken(
                user: user,
                tokenHash: "active",
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: LaterUtc.AddHours(2));
            revokedOldest.Revoke(
                reason: RefreshTokenRevocationReason.AdminRevoked,
                revokedAtUtc: CreatedAtUtc.AddMinutes(5));
            revokedNext.Revoke(
                reason: RefreshTokenRevocationReason.AdminRevoked,
                revokedAtUtc: CreatedAtUtc.AddMinutes(15));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.SaveChangesAsync();

            int expiredDeleted = await repository.DeleteExpiredBatchAsync(
                expiredBeforeUtc: CreatedAtUtc.AddMinutes(40),
                batchSize: 2,
                cancellationToken: CancellationToken.None);
            string[] afterExpired = await database.DbContext.Users
               .SelectMany(x => x.RefreshTokens)
               .AsNoTracking()
               .Select(x => x.TokenHash)
               .OrderBy(x => x)
               .ToArrayAsync();

            Assert.Equal(
                expected: 2,
                actual: expiredDeleted);
            Assert.DoesNotContain(
                expected: "expired-1",
                collection: afterExpired);
            Assert.DoesNotContain(
                expected: "expired-2",
                collection: afterExpired);
            Assert.Contains(
                expected: "expired-3",
                collection: afterExpired);

            int revokedDeleted = await repository.DeleteRevokedBatchAsync(
                revokedBeforeUtc: CreatedAtUtc.AddMinutes(20),
                batchSize: 1,
                cancellationToken: CancellationToken.None);
            string[] afterRevoked = await database.DbContext.Users
               .SelectMany(x => x.RefreshTokens)
               .AsNoTracking()
               .Select(x => x.TokenHash)
               .OrderBy(x => x)
               .ToArrayAsync();

            Assert.Equal(
                expected: 1,
                actual: revokedDeleted);
            Assert.DoesNotContain(
                expected: "revoked-1",
                collection: afterRevoked);
            Assert.Contains(
                expected: "revoked-2",
                collection: afterRevoked);
            Assert.Contains(
                expected: "active",
                collection: afterRevoked);
        }
    }
}
