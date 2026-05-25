using Matrix.Identity.Api.Controllers.Admin;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetDefaultUserAccessPermissions;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Matrix.Identity.Application.UseCases.Admin.Permissions.UpdateDefaultUserAccessPermissions;
using Matrix.Identity.Contracts.Admin.Permissions.Requests;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Admin
{
    public sealed class AdminPermissionsControllerTests
    {
        [Fact]
        public async Task GetPermissions_MapsCatalogItems()
        {
            var sender = new FakeSender();
            sender.Handle<GetPermissionsCatalogQuery, IReadOnlyCollection<PermissionCatalogItemResult>>(_ =>
            [
                new PermissionCatalogItemResult
                {
                    Key = "identity.users.read",
                    Service = "identity",
                    Group = "users",
                    Description = "Read users",
                    IsDeprecated = false
                },
                new PermissionCatalogItemResult
                {
                    Key = "identity.users.delete",
                    Service = "identity",
                    Group = "users",
                    Description = "Delete users",
                    IsDeprecated = true
                }
            ]);
            var controller = new AdminPermissionsController(sender);

            ActionResult<IReadOnlyCollection<PermissionCatalogItemResponse>> actionResult =
                await controller.GetPermissions(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            IReadOnlyCollection<PermissionCatalogItemResponse> response =
                Assert.IsAssignableFrom<IReadOnlyCollection<PermissionCatalogItemResponse>>(ok.Value);

            Assert.Collection(
                collection: response,
                item =>
                {
                    Assert.Equal(
                        expected: "identity.users.read",
                        actual: item.Key);
                    Assert.Equal(
                        expected: "identity",
                        actual: item.Service);
                    Assert.False(item.IsDeprecated);
                },
                item =>
                {
                    Assert.Equal(
                        expected: "identity.users.delete",
                        actual: item.Key);
                    Assert.True(item.IsDeprecated);
                });
        }

        [Fact]
        public async Task GetDefaultUserAccessPermissions_MapsVersionAndKeys()
        {
            var sender = new FakeSender();
            sender.Handle<GetDefaultUserAccessPermissionsQuery, DefaultUserAccessPermissionsResult>(_ =>
                new DefaultUserAccessPermissionsResult(
                    Version: 7,
                    PermissionKeys:
                    [
                        "identity.me.read",
                        "identity.me.write"
                    ]));
            var controller = new AdminPermissionsController(sender);

            ActionResult<DefaultUserAccessPermissionsResponse> actionResult =
                await controller.GetDefaultUserAccessPermissions(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            DefaultUserAccessPermissionsResponse response =
                Assert.IsType<DefaultUserAccessPermissionsResponse>(ok.Value);

            Assert.Equal(
                expected: 7,
                actual: response.Version);
            Assert.Equal(
                expected:
                [
                    "identity.me.read",
                    "identity.me.write"
                ],
                actual: response.PermissionKeys);
        }

        [Fact]
        public async Task UpdateDefaultUserAccessPermissions_UsesRequestKeys()
        {
            var sender = new FakeSender();
            sender.Handle<UpdateDefaultUserAccessPermissionsCommand>(_ => { });
            var controller = new AdminPermissionsController(sender);

            IActionResult result = await controller.UpdateDefaultUserAccessPermissions(
                request: new UpdateDefaultUserAccessPermissionsRequest
                {
                    PermissionKeys =
                    [
                        "identity.me.read",
                        "identity.me.write"
                    ]
                },
                cancellationToken: CancellationToken.None);

            UpdateDefaultUserAccessPermissionsCommand command =
                Assert.IsType<UpdateDefaultUserAccessPermissionsCommand>(sender.Requests.Single());

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(
                expected:
                [
                    "identity.me.read",
                    "identity.me.write"
                ],
                actual: command.PermissionKeys);
        }
    }
}
