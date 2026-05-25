using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class UserAdminReadRepositoryTests
    {
        [Fact]
        public async Task GetPageAsync_ReturnsUsersOrderedByCreationDateWithLastVisitedAtUtc()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserAdminReadRepository(database.DbContext);
            User older = CreateUser(
                email: "older@matrix.local",
                username: "older",
                createdAtUtc: CreatedAtUtc);
            older.ChangeAvatar("https://cdn/older.png");
            older.ConfirmEmail(CreatedAtUtc.AddMinutes(1));
            User newer = CreateUser(
                email: "newer@matrix.local",
                username: "newer",
                createdAtUtc: LaterUtc);
            newer.Lock();
            newer.SoftDelete(LaterUtc.AddMinutes(1));
            UserSession olderSession = CreateSession(
                userId: older.Id,
                createdAtUtc: CreatedAtUtc,
                expiresAtUtc: CreatedAtUtc.AddHours(5));
            olderSession.Touch(
                deviceInfo: CreateDeviceInfo(),
                geoLocation: CreateGeoLocation(),
                refreshTokenExpiresAtUtc: CreatedAtUtc.AddHours(5),
                isPersistent: true,
                touchedAtUtc: CreatedAtUtc.AddHours(2));
            UserSession newerSession = CreateSession(
                userId: newer.Id,
                createdAtUtc: LaterUtc,
                expiresAtUtc: LaterUtc.AddHours(5));

            await database.DbContext.Users.AddRangeAsync(
                older,
                newer);
            await database.DbContext.UserSessions.AddRangeAsync(
                olderSession,
                newerSession);
            await database.DbContext.SaveChangesAsync();

            PagedResult<UserListItemResult> page = await repository.GetPageAsync(
                pagination: new Pagination(
                    pageNumber: 1,
                    pageSize: 10),
                cancellationToken: CancellationToken.None);
            UserListItemResult[] items = page.Items.ToArray();

            Assert.Equal(
                expected: 2,
                actual: page.TotalCount);
            Assert.Equal(
                expectedSpan:
                [
                    newer.Id,
                    older.Id
                ],
                actualArray: items.Select(x => x.Id)
                   .ToArray());
            Assert.Equal(
                expected: LaterUtc,
                actual: items[0].LastVisitedAtUtc);
            Assert.Equal(
                expected: CreatedAtUtc.AddHours(2),
                actual: items[1].LastVisitedAtUtc);
            Assert.True(items[0].IsDeleted);
            Assert.True(items[0].IsLocked);
        }

        [Fact]
        public async Task GetRoleMembersPageAsync_ReturnsOnlyUsersAssignedToRole()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserAdminReadRepository(database.DbContext);
            Role role = CreateRole("Moderator");
            User included = CreateUser(
                email: "included@matrix.local",
                username: "included",
                createdAtUtc: LaterUtc);
            User excluded = CreateUser(
                email: "excluded@matrix.local",
                username: "excluded",
                createdAtUtc: CreatedAtUtc);

            await database.DbContext.Roles.AddAsync(role);
            await database.DbContext.Users.AddRangeAsync(
                included,
                excluded);
            await database.DbContext.UserRoles.AddAsync(
                new UserRole(
                    userId: included.Id,
                    roleId: role.Id));
            await database.DbContext.SaveChangesAsync();

            PagedResult<UserListItemResult> page = await repository.GetRoleMembersPageAsync(
                roleId: role.Id,
                pagination: new Pagination(
                    pageNumber: 1,
                    pageSize: 10),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: page.TotalCount);
            Assert.Equal(
                expectedSpan: [included.Id],
                actualArray: page.Items.Select(x => x.Id)
                   .ToArray());
        }
    }
}
