using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class OneTimeTokenRepositoryTests
    {
        [Fact]
        public async Task GetActive_ReturnsOnlyNonRevokedUnusedNonExpiredTokensInDescendingCreationOrder()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new OneTimeTokenRepository(database.DbContext);
            User user = CreateUser();
            OneTimeToken oldestActive = CreateOneTimeToken(
                userId: user.Id,
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddHours(4));
            OneTimeToken newestActive = CreateOneTimeToken(
                userId: user.Id,
                tokenHash: "active-2",
                createdAtUtc: LaterUtc,
                expiresAtUtc: LaterUtc.AddHours(2));
            OneTimeToken used = CreateOneTimeToken(
                userId: user.Id,
                tokenHash: "used",
                createdAtUtc: LaterUtc.AddMinutes(5),
                expiresAtUtc: LaterUtc.AddHours(2));
            OneTimeToken revoked = CreateOneTimeToken(
                userId: user.Id,
                tokenHash: "revoked",
                createdAtUtc: LaterUtc.AddMinutes(10),
                expiresAtUtc: LaterUtc.AddHours(2));
            OneTimeToken expired = CreateOneTimeToken(
                userId: user.Id,
                tokenHash: "expired",
                createdAtUtc: LaterUtc.AddMinutes(15),
                expiresAtUtc: LaterUtc.AddMinutes(20));

            used.MarkUsed(LaterUtc.AddMinutes(30));
            revoked.Revoke(LaterUtc.AddMinutes(30));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.OneTimeTokens.AddRangeAsync(
                oldestActive,
                newestActive,
                used,
                revoked,
                expired);
            await database.DbContext.SaveChangesAsync();

            IReadOnlyList<OneTimeToken> active = await repository.GetActive(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.PasswordReset,
                nowUtc: LaterUtc.AddHours(1),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    newestActive.Id,
                    oldestActive.Id
                ],
                actualArray: active.Select(x => x.Id)
                   .ToArray());
        }

        [Fact]
        public async Task GetLatestCreatedAtUtc_AndCountCreatedSinceUtc_ReturnExpectedValues()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new OneTimeTokenRepository(database.DbContext);
            User user = CreateUser();
            OneTimeToken first = CreateOneTimeToken(
                userId: user.Id,
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddHours(1));
            OneTimeToken second = CreateOneTimeToken(
                userId: user.Id,
                tokenHash: "token-2",
                createdAtUtc: LaterUtc,
                expiresAtUtc: LaterUtc.AddHours(1));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.OneTimeTokens.AddRangeAsync(
                first,
                second);
            await database.DbContext.SaveChangesAsync();

            DateTime? latestCreatedAtUtc = await repository.GetLatestCreatedAtUtc(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.PasswordReset,
                cancellationToken: CancellationToken.None);
            int createdSince = await repository.CountCreatedSinceUtc(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.PasswordReset,
                sinceUtc: CreatedAtUtc.AddMinutes(30),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: LaterUtc,
                actual: latestCreatedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: createdSince);
        }
    }
}
