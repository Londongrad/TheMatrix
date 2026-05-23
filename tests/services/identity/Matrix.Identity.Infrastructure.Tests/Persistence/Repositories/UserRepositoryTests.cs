using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories;

public sealed class UserRepositoryTests
{
    [Fact]
    public async Task GetByEmailAsync_WhenUserHasRefreshTokens_ReturnsUserWithOwnedCollection()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserRepository(database.DbContext);
        User user = CreateUser();
        user.RequestEmailChange(Email.Create("trinity@matrix.local"), LaterUtc);
        RefreshToken refreshToken = IssueRefreshToken(user, tokenHash: "refresh-hash-1");

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        User? byEmail = await repository.GetByEmailAsync(user.Email.Value);
        User? byPendingEmail = await repository.GetByPendingEmailAsync("trinity@matrix.local");
        User? byTokenHash = await repository.GetByRefreshTokenHashAsync(refreshToken.TokenHash);

        Assert.NotNull(byEmail);
        Assert.NotNull(byPendingEmail);
        Assert.NotNull(byTokenHash);
        Assert.Single(byEmail.RefreshTokens);
        Assert.Equal(refreshToken.TokenHash, Assert.Single(byTokenHash.RefreshTokens).TokenHash);
    }

    [Fact]
    public async Task BumpPermissionsVersionAsync_WhenUserExists_IncrementsPermissionsVersion()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserRepository(database.DbContext);
        User user = CreateUser();

        await database.DbContext.Users.AddAsync(user);
        await database.DbContext.SaveChangesAsync();

        bool updated = await repository.BumpPermissionsVersionAsync(user.Id, CancellationToken.None);
        int? version = await repository.GetPermissionsVersionAsync(user.Id, CancellationToken.None);

        Assert.True(updated);
        Assert.Equal(2, version);
    }

    [Fact]
    public async Task BumpPermissionsVersionByRoleAsync_WhenRoleHasMembers_UpdatesOnlyMatchingUsers()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserRepository(database.DbContext);
        Role role = CreateRole("Moderator");
        User matchingUser = CreateUser("morpheus@matrix.local", "morpheus");
        User otherUser = CreateUser("smith@matrix.local", "smith");

        await database.DbContext.Roles.AddAsync(role);
        await database.DbContext.Users.AddRangeAsync(matchingUser, otherUser);
        await database.DbContext.UserRoles.AddAsync(new UserRole(matchingUser.Id, role.Id));
        await database.DbContext.SaveChangesAsync();

        int affected = await repository.BumpPermissionsVersionByRoleAsync(role.Id, CancellationToken.None);
        IReadOnlyCollection<Guid> userIds = await repository.GetUserIdsByRoleAsync(role.Id, CancellationToken.None);
        int? matchingVersion = await repository.GetPermissionsVersionAsync(matchingUser.Id, CancellationToken.None);
        int? otherVersion = await repository.GetPermissionsVersionAsync(otherUser.Id, CancellationToken.None);

        Assert.Equal(1, affected);
        Assert.Equal([matchingUser.Id], userIds);
        Assert.Equal(2, matchingVersion);
        Assert.Equal(1, otherVersion);
    }

    [Fact]
    public async Task StreamUserIdsByRoleAsync_WhenRoleHasMembers_YieldsOnlyMatchingUsers()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserRepository(database.DbContext);
        Role role = CreateRole("Moderator");
        Role otherRole = CreateRole("Other");
        User matchingUser1 = CreateUser("morpheus@matrix.local", "morpheus");
        User matchingUser2 = CreateUser("trinity@matrix.local", "trinity");
        User otherUser = CreateUser("smith@matrix.local", "smith");

        await database.DbContext.Roles.AddRangeAsync(role, otherRole);
        await database.DbContext.Users.AddRangeAsync(matchingUser1, matchingUser2, otherUser);
        await database.DbContext.UserRoles.AddRangeAsync(
            new UserRole(matchingUser1.Id, role.Id),
            new UserRole(matchingUser2.Id, role.Id),
            new UserRole(otherUser.Id, otherRole.Id));
        await database.DbContext.SaveChangesAsync();

        List<Guid> userIds = [];

        await foreach (Guid userId in repository.StreamUserIdsByRoleAsync(role.Id, CancellationToken.None))
            userIds.Add(userId);

        Assert.Equal(
            new[] { matchingUser1.Id, matchingUser2.Id }.OrderBy(id => id).ToArray(),
            userIds.OrderBy(id => id).ToArray());
    }

    [Fact]
    public async Task StreamUserIdsByRoleAsync_WhenRoleHasNoMembers_YieldsEmptySequence()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new UserRepository(database.DbContext);
        Role role = CreateRole("Auditor");

        await database.DbContext.Roles.AddAsync(role);
        await database.DbContext.SaveChangesAsync();

        List<Guid> userIds = [];

        await foreach (Guid userId in repository.StreamUserIdsByRoleAsync(role.Id, CancellationToken.None))
            userIds.Add(userId);

        Assert.Empty(userIds);
    }
}
