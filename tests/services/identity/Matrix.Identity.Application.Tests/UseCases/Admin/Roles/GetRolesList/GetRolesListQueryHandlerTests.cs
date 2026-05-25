using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRolesList
{
    public sealed class GetRolesListQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsRolesFromRepository()
        {
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.Roles.AddRange(
            [
                new RoleListItemResult
                {
                    Id = Guid.NewGuid(),
                    Name = "Operators",
                    IsSystem = false,
                    CreatedAtUtc = AdminRolesTestSupport.UtcNow.AddDays(-2)
                },
                new RoleListItemResult
                {
                    Id = Guid.NewGuid(),
                    Name = "SuperAdmin",
                    IsSystem = true,
                    CreatedAtUtc = AdminRolesTestSupport.UtcNow.AddDays(-5)
                }
            ]);
            var handler = new GetRolesListQueryHandler(roleReadRepository);

            IReadOnlyCollection<RoleListItemResult> result = await handler.Handle(
                request: new GetRolesListQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: result.Count);
            Assert.Equal(
                expected: new[]
                {
                    "Operators",
                    "SuperAdmin"
                },
                actual: result.Select(x => x.Name));
        }
    }
}
