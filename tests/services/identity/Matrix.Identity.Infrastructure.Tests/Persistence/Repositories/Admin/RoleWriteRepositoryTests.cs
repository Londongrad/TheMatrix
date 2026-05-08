using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Repositories.Admin;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Persistence.Repositories.Admin;

public sealed class RoleWriteRepositoryTests
{
    [Fact]
    public async Task AddGetAndDeleteAsync_ManipulatesRoleEntities()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var repository = new RoleWriteRepository(database.DbContext);
        Role role = CreateRole("Moderator");

        await repository.AddAsync(role, CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        Role? loaded = await repository.GetByIdForUpdateAsync(role.Id, CancellationToken.None);
        Assert.NotNull(loaded);

        await repository.DeleteAsync(loaded, CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        Assert.Null(await repository.GetByIdForUpdateAsync(role.Id, CancellationToken.None));
    }
}
