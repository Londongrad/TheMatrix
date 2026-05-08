using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class UserRolesRepositoryTests
{
    [Fact]
    public async Task ReplaceUserRolesAsync_UpdatesRoleSet_AndGetUserRolesAsyncReturnsOrderedProjection()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserRolesRepository(database.DbContext);
        User user = CreateUser();
        Role moderator = CreateRole("Moderator");
        Role userRole = CreateRole("User");
        Role auditor = CreateRole("Auditor");

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.Roles.AddRangeAsync(moderator, userRole, auditor);
        await database.DbContext.UserRoles.AddRangeAsync(
            new UserRole(user.Id, moderator.Id),
            new UserRole(user.Id, userRole.Id));
        await database.DbContext.SaveChangesAsync();

        bool changed = await repository.ReplaceUserRolesAsync(
            user.Id,
            [auditor.Id, moderator.Id],
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();
        var roles = await repository.GetUserRolesAsync(user.Id, CancellationToken.None);

        Assert.True(changed);
        Assert.Equal(["Auditor", "Moderator"], roles.Select(x => x.Name).ToArray());
    }
}
