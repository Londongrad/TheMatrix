using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class UserSessionRepositoryTests
    {
        [Fact]
        public async Task GetActiveByUserIdAndDeviceIdAsync_ReturnsMostRecentlyUsedActiveSession()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserSessionRepository(database.DbContext);
            User user = CreateUser();
            UserSession older = CreateSession(
                userId: user.Id,
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddHours(6));
            UserSession newer = CreateSession(
                userId: user.Id,
                createdAtUtc: LaterUtc,
                expiresAtUtc: LaterUtc.AddHours(6));
            newer.Touch(
                deviceInfo: CreateDeviceInfo(),
                geoLocation: CreateGeoLocation(),
                refreshTokenExpiresAtUtc: LaterUtc.AddHours(6),
                isPersistent: true,
                touchedAtUtc: LaterUtc.AddHours(1));

            await database.DbContext.Users.AddAsync(user);
            await repository.AddAsync(older);
            await repository.AddAsync(newer);
            await database.DbContext.SaveChangesAsync();

            UserSession? result = await repository.GetActiveByUserIdAndDeviceIdAsync(
                userId: user.Id,
                deviceId: "device-1",
                utcNow: LaterUtc,
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: newer.Id,
                actual: result.Id);
        }

        [Fact]
        public async Task GetEndedPageByUserIdAsync_ReturnsRevokedAndExpiredSessionsOrderedByLatestVisit()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserSessionRepository(database.DbContext);
            User user = CreateUser();
            UserSession revoked = CreateSession(
                userId: user.Id,
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddHours(3));
            UserSession expired = CreateSession(
                userId: user.Id,
                deviceId: "device-2",
                createdAtUtc: LaterUtc,
                expiresAtUtc: LaterUtc.AddMinutes(30));
            UserSession active = CreateSession(
                userId: user.Id,
                deviceId: "device-3",
                createdAtUtc: LaterUtc.AddHours(1),
                expiresAtUtc: LaterUtc.AddHours(5));

            revoked.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: LaterUtc);
            expired.Touch(
                deviceInfo: CreateDeviceInfo("device-2"),
                geoLocation: CreateGeoLocation(),
                refreshTokenExpiresAtUtc: expired.RefreshTokenExpiresAtUtc,
                isPersistent: true,
                touchedAtUtc: LaterUtc.AddMinutes(10));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.UserSessions.AddRangeAsync(
                revoked,
                expired,
                active);
            await database.DbContext.SaveChangesAsync();

            (IReadOnlyCollection<UserSession> items, int totalCount) = await repository.GetEndedPageByUserIdAsync(
                userId: user.Id,
                utcNow: LaterUtc.AddHours(3),
                pagination: new Pagination(
                    pageNumber: 1,
                    pageSize: 10),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: totalCount);
            Assert.Equal(
                expectedSpan:
                [
                    expired.Id,
                    revoked.Id
                ],
                actualArray: items.Select(x => x.Id)
                   .ToArray());
        }

        [Fact]
        public async Task GetLastVisitedAtUtcAsync_UsesLastUsedAtUtcWhenPresent()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserSessionRepository(database.DbContext);
            User user = CreateUser();
            UserSession createdOnly = CreateSession(
                userId: user.Id,
                createdAtUtc: CreatedAtUtc);
            UserSession touched = CreateSession(
                userId: user.Id,
                deviceId: "device-2",
                createdAtUtc: LaterUtc);
            touched.Touch(
                deviceInfo: CreateDeviceInfo("device-2"),
                geoLocation: CreateGeoLocation(),
                refreshTokenExpiresAtUtc: touched.RefreshTokenExpiresAtUtc,
                isPersistent: true,
                touchedAtUtc: LaterUtc.AddHours(4));

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.UserSessions.AddRangeAsync(
                createdOnly,
                touched);
            await database.DbContext.SaveChangesAsync();

            DateTime? lastVisitedAtUtc = await repository.GetLastVisitedAtUtcAsync(
                userId: user.Id,
                cancellationToken: CancellationToken.None);
            IReadOnlyCollection<UserSession> byDevice = await repository.ListByUserIdAndDeviceIdAsync(
                userId: user.Id,
                deviceId: "device-2",
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: LaterUtc.AddHours(4),
                actual: lastVisitedAtUtc);
            Assert.Equal(
                expectedSpan: [touched.Id],
                actualArray: byDevice.Select(x => x.Id)
                   .ToArray());
        }
    }
}
