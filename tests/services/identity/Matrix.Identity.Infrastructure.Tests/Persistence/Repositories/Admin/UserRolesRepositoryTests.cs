using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using Xunit;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin
{
    public sealed class UserRolesRepositoryTests
    {
        [Fact]
        public async Task ReplaceUserRolesAsync_UpdatesRoleSet_AndGetUserRolesAsyncReturnsOrderedProjection()
        {
            await using IdentityTestDatabase database = CreateDbContext();
            var repository = new UserRolesRepository(database.DbContext);
            User user = CreateUser();
            Role moderator = CreateRole("Moderator");
            Role userRole = CreateRole();
            Role auditor = CreateRole("Auditor");

            await database.DbContext.Users.AddAsync(user);
            await database.DbContext.Roles.AddRangeAsync(
                moderator,
                userRole,
                auditor);
            await database.DbContext.UserRoles.AddRangeAsync(
                new UserRole(
                    userId: user.Id,
                    roleId: moderator.Id),
                new UserRole(
                    userId: user.Id,
                    roleId: userRole.Id));
            await database.DbContext.SaveChangesAsync();

            bool changed = await repository.ReplaceUserRolesAsync(
                userId: user.Id,
                roleIds:
                [
                    auditor.Id,
                    moderator.Id
                ],
                cancellationToken: CancellationToken.None);
            await database.DbContext.SaveChangesAsync();
            IReadOnlyCollection<UserRoleResult> roles = await repository.GetUserRolesAsync(
                userId: user.Id,
                cancellationToken: CancellationToken.None);

            Assert.True(changed);
            Assert.Equal(
                expectedSpan:
                [
                    "Auditor",
                    "Moderator"
                ],
                actualArray: roles.Select(x => x.Name)
                   .ToArray());
        }
    }
}
