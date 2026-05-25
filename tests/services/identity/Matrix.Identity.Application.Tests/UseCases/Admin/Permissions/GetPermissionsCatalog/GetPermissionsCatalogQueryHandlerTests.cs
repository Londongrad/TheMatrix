using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Permissions.GetPermissionsCatalog
{
    public sealed class GetPermissionsCatalogQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsPermissionsFromRepository()
        {
            var permissionReadRepository = new AdminUsersTestSupport.FakePermissionReadRepository
            {
                Permissions =
                [
                    new PermissionCatalogItemResult
                    {
                        Key = "users.read",
                        Service = "identity",
                        Group = "users",
                        Description = "Read users."
                    },
                    new PermissionCatalogItemResult
                    {
                        Key = "roles.manage",
                        Service = "identity",
                        Group = "roles",
                        Description = "Manage roles."
                    }
                ]
            };
            var handler = new GetPermissionsCatalogQueryHandler(permissionReadRepository);

            IReadOnlyCollection<PermissionCatalogItemResult> result = await handler.Handle(
                request: new GetPermissionsCatalogQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected:
                [
                    "users.read",
                    "roles.manage"
                ],
                actual: result.Select(x => x.Key));
        }
    }
}
