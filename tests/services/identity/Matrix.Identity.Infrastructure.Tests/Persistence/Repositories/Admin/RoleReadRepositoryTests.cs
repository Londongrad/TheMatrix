using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class RoleReadRepositoryTests
    {
        [Fact]
        public async Task GetRolesAsync_ExcludesSuperAdminAndOrdersByName()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new RoleReadRepository(database.DbContext);
            Role system = CreateRole(
                name: SystemRoleNames.SuperAdmin,
                isSystem: true,
                createdAtUtc: CreatedAtUtc);
            Role user = CreateRole(
                name: "User",
                createdAtUtc: LaterUtc);
            Role moderator = CreateRole(
                name: "Moderator",
                createdAtUtc: LaterUtc.AddMinutes(1));

            await database.DbContext.Roles.AddRangeAsync(
                system,
                user,
                moderator);
            await database.DbContext.SaveChangesAsync();

            IReadOnlyCollection<RoleListItemResult> roles = await repository.GetRolesAsync(CancellationToken.None);

            Assert.Equal(
                expectedSpan:
                [
                    "Moderator",
                    "User"
                ],
                actualArray: roles.Select(x => x.Name)
                   .ToArray());
        }

        [Fact]
        public async Task ExistsAndGetMethods_UseNormalizedRoleName()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new RoleReadRepository(database.DbContext);
            Role role = CreateRole("Moderator");
            Role otherRole = CreateRole("Operator");

            await database.DbContext.Roles.AddRangeAsync(
                role,
                otherRole);
            await database.DbContext.SaveChangesAsync();

            bool existsByName = await repository.ExistsByNameAsync(
                roleName: " moderator ",
                cancellationToken: CancellationToken.None);
            bool existsByNameExcept = await repository.ExistsByNameExceptAsync(
                roleName: " moderator ",
                excludedRoleId: otherRole.Id,
                cancellationToken: CancellationToken.None);
            Role? byName = await repository.GetByNameAsync(
                roleName: " MODERATOR ",
                cancellationToken: CancellationToken.None);
            IReadOnlyCollection<Guid> existingRoleIds = await repository.GetExistingRoleIdsAsync(
                roleIds:
                [
                    role.Id,
                    Guid.NewGuid()
                ],
                cancellationToken: CancellationToken.None);

            Assert.True(existsByName);
            Assert.True(existsByNameExcept);
            Assert.Equal(
                expected: role.Id,
                actual: byName!.Id);
            Assert.Equal(
                expected: [role.Id],
                actual: existingRoleIds);
        }
    }
}
