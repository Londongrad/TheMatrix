using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class UserAdminReadRepositoryTests
{
    [Fact]
    public async Task GetPageAsync_ReturnsUsersOrderedByCreationDateWithLastVisitedAtUtc()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserAdminReadRepository(database.DbContext);
        User older = CreateUser("older@matrix.local", "older", CreatedAtUtc);
        older.ChangeAvatar("https://cdn/older.png");
        older.ConfirmEmail(CreatedAtUtc.AddMinutes(1));
        User newer = CreateUser("newer@matrix.local", "newer", LaterUtc);
        newer.Lock();
        newer.SoftDelete(LaterUtc.AddMinutes(1));
        UserSession olderSession = CreateSession(older.Id, createdAtUtc: CreatedAtUtc, expiresAtUtc: CreatedAtUtc.AddHours(5));
        olderSession.Touch(CreateDeviceInfo(), CreateGeoLocation(), CreatedAtUtc.AddHours(5), true, CreatedAtUtc.AddHours(2));
        UserSession newerSession = CreateSession(newer.Id, createdAtUtc: LaterUtc, expiresAtUtc: LaterUtc.AddHours(5));

        await database.DbContext.Users.AddRangeAsync(older, newer);
        await database.DbContext.UserSessions.AddRangeAsync(olderSession, newerSession);
        await database.DbContext.SaveChangesAsync();

        PagedResult<UserListItemResult> page = await repository.GetPageAsync(
            new Pagination(1, 10),
            CancellationToken.None);
        UserListItemResult[] items = page.Items.ToArray();

        Assert.Equal(2, page.TotalCount);
        Assert.Equal([newer.Id, older.Id], items.Select(x => x.Id).ToArray());
        Assert.Equal(LaterUtc, items[0].LastVisitedAtUtc);
        Assert.Equal(CreatedAtUtc.AddHours(2), items[1].LastVisitedAtUtc);
        Assert.True(items[0].IsDeleted);
        Assert.True(items[0].IsLocked);
    }

    [Fact]
    public async Task GetRoleMembersPageAsync_ReturnsOnlyUsersAssignedToRole()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserAdminReadRepository(database.DbContext);
        Role role = CreateRole("Moderator");
        User included = CreateUser("included@matrix.local", "included", LaterUtc);
        User excluded = CreateUser("excluded@matrix.local", "excluded", CreatedAtUtc);

        await database.DbContext.Roles.AddAsync(role);
        await database.DbContext.Users.AddRangeAsync(included, excluded);
        await database.DbContext.UserRoles.AddAsync(new UserRole(included.Id, role.Id));
        await database.DbContext.SaveChangesAsync();

        PagedResult<UserListItemResult> page = await repository.GetRoleMembersPageAsync(
            role.Id,
            new Pagination(1, 10),
            CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal([included.Id], page.Items.Select(x => x.Id).ToArray());
    }
}
