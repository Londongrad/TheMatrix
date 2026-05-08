using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class RoleReadRepositoryTests
{
    [Fact]
    public async Task GetRolesAsync_ExcludesSuperAdminAndOrdersByName()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new RoleReadRepository(database.DbContext);
        Role system = CreateRole(SystemRoleNames.SuperAdmin, isSystem: true, createdAtUtc: CreatedAtUtc);
        Role user = CreateRole("User", createdAtUtc: LaterUtc);
        Role moderator = CreateRole("Moderator", createdAtUtc: LaterUtc.AddMinutes(1));

        await database.DbContext.Roles.AddRangeAsync(system, user, moderator);
        await database.DbContext.SaveChangesAsync();

        var roles = await repository.GetRolesAsync(CancellationToken.None);

        Assert.Equal(["Moderator", "User"], roles.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task ExistsAndGetMethods_UseNormalizedRoleName()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new RoleReadRepository(database.DbContext);
        Role role = CreateRole("Moderator");
        Role otherRole = CreateRole("Operator");

        await database.DbContext.Roles.AddRangeAsync(role, otherRole);
        await database.DbContext.SaveChangesAsync();

        bool existsByName = await repository.ExistsByNameAsync(" moderator ", CancellationToken.None);
        bool existsByNameExcept = await repository.ExistsByNameExceptAsync(" moderator ", otherRole.Id, CancellationToken.None);
        Role? byName = await repository.GetByNameAsync(" MODERATOR ", CancellationToken.None);
        IReadOnlyCollection<Guid> existingRoleIds = await repository.GetExistingRoleIdsAsync([role.Id, Guid.NewGuid()], CancellationToken.None);

        Assert.True(existsByName);
        Assert.True(existsByNameExcept);
        Assert.Equal(role.Id, byName!.Id);
        Assert.Equal([role.Id], existingRoleIds);
    }
}
