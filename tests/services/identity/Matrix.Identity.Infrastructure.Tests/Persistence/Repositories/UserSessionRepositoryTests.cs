using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories;

public sealed class UserSessionRepositoryTests
{
    [Fact]
    public async Task GetActiveByUserIdAndDeviceIdAsync_ReturnsMostRecentlyUsedActiveSession()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserSessionRepository(database.DbContext);
        User user = CreateUser();
        UserSession older = CreateSession(user.Id, createdAtUtc: CreatedAtUtc, expiresAtUtc: CreatedAtUtc.AddHours(6));
        UserSession newer = CreateSession(user.Id, createdAtUtc: LaterUtc, expiresAtUtc: LaterUtc.AddHours(6));
        newer.Touch(CreateDeviceInfo(), CreateGeoLocation(), LaterUtc.AddHours(6), true, LaterUtc.AddHours(1));

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
        Assert.Equal(newer.Id, result.Id);
    }

    [Fact]
    public async Task GetEndedPageByUserIdAsync_ReturnsRevokedAndExpiredSessionsOrderedByLatestVisit()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserSessionRepository(database.DbContext);
        User user = CreateUser();
        UserSession revoked = CreateSession(user.Id, createdAtUtc: CreatedAtUtc, expiresAtUtc: CreatedAtUtc.AddHours(3));
        UserSession expired = CreateSession(user.Id, deviceId: "device-2", createdAtUtc: LaterUtc, expiresAtUtc: LaterUtc.AddMinutes(30));
        UserSession active = CreateSession(user.Id, deviceId: "device-3", createdAtUtc: LaterUtc.AddHours(1), expiresAtUtc: LaterUtc.AddHours(5));

        revoked.Revoke(RefreshTokenRevocationReason.UserRevoked, LaterUtc);
        expired.Touch(CreateDeviceInfo("device-2"), CreateGeoLocation(), expired.RefreshTokenExpiresAtUtc, true, LaterUtc.AddMinutes(10));

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.UserSessions.AddRangeAsync(revoked, expired, active);
        await database.DbContext.SaveChangesAsync();

        (IReadOnlyCollection<UserSession> items, int totalCount) = await repository.GetEndedPageByUserIdAsync(
            userId: user.Id,
            utcNow: LaterUtc.AddHours(3),
            pagination: new Pagination(1, 10),
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, totalCount);
        Assert.Equal([expired.Id, revoked.Id], items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetLastVisitedAtUtcAsync_UsesLastUsedAtUtcWhenPresent()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserSessionRepository(database.DbContext);
        User user = CreateUser();
        UserSession createdOnly = CreateSession(user.Id, createdAtUtc: CreatedAtUtc);
        UserSession touched = CreateSession(user.Id, deviceId: "device-2", createdAtUtc: LaterUtc);
        touched.Touch(CreateDeviceInfo("device-2"), CreateGeoLocation(), touched.RefreshTokenExpiresAtUtc, true, LaterUtc.AddHours(4));

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.UserSessions.AddRangeAsync(createdOnly, touched);
        await database.DbContext.SaveChangesAsync();

        DateTime? lastVisitedAtUtc = await repository.GetLastVisitedAtUtcAsync(user.Id, CancellationToken.None);
        IReadOnlyCollection<UserSession> byDevice = await repository.ListByUserIdAndDeviceIdAsync(
            user.Id,
            "device-2",
            CancellationToken.None);

        Assert.Equal(LaterUtc.AddHours(4), lastVisitedAtUtc);
        Assert.Equal([touched.Id], byDevice.Select(x => x.Id).ToArray());
    }
}
