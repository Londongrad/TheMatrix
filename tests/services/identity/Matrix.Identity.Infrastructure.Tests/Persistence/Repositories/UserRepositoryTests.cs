using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Matrix.Identity.Infrastructure.Persistence.Repositories;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories
{
    public sealed class UserRepositoryTests
    {
        [Fact]
        public async Task GetByEmailAsync_WhenUserHasRefreshTokens_ReturnsUserWithOwnedCollection()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserRepository(database.DbContext);
            User user = CreateUser();
            user.RequestEmailChange(
                newEmail: Email.Create("trinity@matrix.local"),
                requestedAtUtc: LaterUtc);
            RefreshToken refreshToken = IssueRefreshToken(
                user: user,
                tokenHash: "refresh-hash-1");

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.SaveChangesAsync();

            User? byEmail = await repository.GetByEmailAsync(user.Email.Value);
            User? byPendingEmail = await repository.GetByPendingEmailAsync("trinity@matrix.local");
            User? byTokenHash = await repository.GetByRefreshTokenHashAsync(refreshToken.TokenHash);

            Assert.NotNull(byEmail);
            Assert.NotNull(byPendingEmail);
            Assert.NotNull(byTokenHash);
            Assert.Single(byEmail.RefreshTokens);
            Assert.Equal(
                expected: refreshToken.TokenHash,
                actual: Assert.Single(byTokenHash.RefreshTokens)
                   .TokenHash);
        }

        [Fact]
        public async Task BumpPermissionsVersionAsync_WhenUserExists_IncrementsPermissionsVersion()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserRepository(database.DbContext);
            User user = CreateUser();

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.SaveChangesAsync();

            bool updated = await repository.BumpPermissionsVersionAsync(
                userId: user.Id,
                cancellationToken: CancellationToken.None);
            int? version = await repository.GetPermissionsVersionAsync(
                userId: user.Id,
                cancellationToken: CancellationToken.None);

            Assert.True(updated);
            Assert.Equal(
                expected: 2,
                actual: version);
        }

        [Fact]
        public async Task BumpPermissionsVersionByRoleAsync_WhenRoleHasMembers_UpdatesOnlyMatchingUsers()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserRepository(database.DbContext);
            Role role = CreateRole("Moderator");
            User matchingUser = CreateUser(
                email: "morpheus@matrix.local",
                username: "morpheus");
            User otherUser = CreateUser(
                email: "smith@matrix.local",
                username: "smith");

            await database.DbContext.Roles.AddAsync(role);
            await database.DbContext.Users.AddRangeAsync(
                matchingUser,
                otherUser);
            await database.DbContext.UserRoles.AddAsync(
                new UserRole(
                    userId: matchingUser.Id,
                    roleId: role.Id));
            await database.DbContext.SaveChangesAsync();

            int affected = await repository.BumpPermissionsVersionByRoleAsync(
                roleId: role.Id,
                cancellationToken: CancellationToken.None);
            List<Guid> userIds = [];

            await foreach (Guid userId in repository.StreamUserIdsByRoleAsync(
                               roleId: role.Id,
                               cancellationToken: CancellationToken.None))
                userIds.Add(userId);

            int? matchingVersion = await repository.GetPermissionsVersionAsync(
                userId: matchingUser.Id,
                cancellationToken: CancellationToken.None);
            int? otherVersion = await repository.GetPermissionsVersionAsync(
                userId: otherUser.Id,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: affected);
            Assert.Equal(
                expected: [matchingUser.Id],
                actual: userIds);
            Assert.Equal(
                expected: 2,
                actual: matchingVersion);
            Assert.Equal(
                expected: 1,
                actual: otherVersion);
        }

        [Fact]
        public async Task StreamUserIdsByRoleAsync_WhenRoleHasMembers_YieldsOnlyMatchingUsers()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserRepository(database.DbContext);
            Role role = CreateRole("Moderator");
            Role otherRole = CreateRole("Other");
            User matchingUser1 = CreateUser(
                email: "morpheus@matrix.local",
                username: "morpheus");
            User matchingUser2 = CreateUser(
                email: "trinity@matrix.local",
                username: "trinity");
            User otherUser = CreateUser(
                email: "smith@matrix.local",
                username: "smith");

            await database.DbContext.Roles.AddRangeAsync(
                role,
                otherRole);
            await database.DbContext.Users.AddRangeAsync(
                matchingUser1,
                matchingUser2,
                otherUser);
            await database.DbContext.UserRoles.AddRangeAsync(
                new UserRole(
                    userId: matchingUser1.Id,
                    roleId: role.Id),
                new UserRole(
                    userId: matchingUser2.Id,
                    roleId: role.Id),
                new UserRole(
                    userId: otherUser.Id,
                    roleId: otherRole.Id));
            await database.DbContext.SaveChangesAsync();

            List<Guid> userIds = [];

            await foreach (Guid userId in repository.StreamUserIdsByRoleAsync(
                               roleId: role.Id,
                               cancellationToken: CancellationToken.None))
                userIds.Add(userId);

            Assert.Equal(
                expected: new[]
                    {
                        matchingUser1.Id,
                        matchingUser2.Id
                    }.OrderBy(id => id)
                   .ToArray(),
                actual: userIds.OrderBy(id => id)
                   .ToArray());
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

            await foreach (Guid userId in repository.StreamUserIdsByRoleAsync(
                               roleId: role.Id,
                               cancellationToken: CancellationToken.None))
                userIds.Add(userId);

            Assert.Empty(userIds);
        }
    }
}
