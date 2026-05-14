using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Api.Controllers.Admin;
using Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole;
using Matrix.Identity.Application.UseCases.Admin.Roles.DeleteRole;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolesList;
using Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole;
using Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Admin;

public sealed class AdminRolesControllerTests
{
    [Fact]
    public async Task GetRoles_CreateRole_AndRenameRole_MapResponses()
    {
        Guid roleId = Guid.Parse("f20f72d9-3ac5-4f7c-b40c-f382758a58d2");
        var sender = new FakeSender();
        sender.Handle<GetRolesListQuery, IReadOnlyCollection<RoleListItemResult>>(_ =>
        [
            new RoleListItemResult
            {
                Id = roleId,
                Name = "Operator",
                IsSystem = false,
                CreatedAtUtc = new DateTime(2048, 6, 1, 6, 0, 0, DateTimeKind.Utc)
            }
        ]);
        sender.Handle<CreateRoleCommand, RoleCreatedResult>(_ => new RoleCreatedResult
        {
            Id = roleId,
            Name = "Operator",
            IsSystem = false,
            CreatedAtUtc = new DateTime(2048, 6, 1, 6, 0, 0, DateTimeKind.Utc)
        });
        sender.Handle<RenameRoleCommand, RoleRenamedResult>(_ => new RoleRenamedResult
        {
            Id = roleId,
            Name = "Senior Operator",
            IsSystem = false,
            CreatedAtUtc = new DateTime(2048, 6, 1, 6, 0, 0, DateTimeKind.Utc)
        });
        var controller = new AdminRolesController(sender);

        ActionResult<IReadOnlyCollection<RoleResponse>> getResult = await controller.GetRoles(CancellationToken.None);
        ActionResult<RoleResponse> createResult = await controller.CreateRole(
            new CreateRoleRequest
            {
                Name = "Operator"
            },
            CancellationToken.None);
        ActionResult<RoleResponse> renameResult = await controller.RenameRole(
            roleId,
            new RenameRoleRequest
            {
                Name = "Senior Operator"
            },
            CancellationToken.None);

        OkObjectResult getOk = Assert.IsType<OkObjectResult>(getResult.Result);
        List<RoleResponse> roles = Assert.IsAssignableFrom<IEnumerable<RoleResponse>>(getOk.Value).ToList();
        Assert.Single(roles);
        Assert.Equal("Operator", roles.Single().Name);

        OkObjectResult createOk = Assert.IsType<OkObjectResult>(createResult.Result);
        RoleResponse created = Assert.IsType<RoleResponse>(createOk.Value);
        Assert.Equal("Operator", created.Name);

        OkObjectResult renameOk = Assert.IsType<OkObjectResult>(renameResult.Result);
        RoleResponse renamed = Assert.IsType<RoleResponse>(renameOk.Value);
        Assert.Equal("Senior Operator", renamed.Name);
    }

    [Fact]
    public async Task DeleteRole_AndUpdatePermissions_UseCommands()
    {
        Guid roleId = Guid.Parse("e12f1c2e-02b2-4ad0-8f8e-79073d148508");
        var sender = new FakeSender();
        sender.Handle<DeleteRoleCommand>(_ => { });
        sender.Handle<UpdateRolePermissionsCommand>(_ => { });
        var controller = new AdminRolesController(sender);

        IActionResult deleteResult = await controller.DeleteRole(roleId, CancellationToken.None);
        IActionResult updateResult = await controller.UpdateRolePermissions(
            roleId,
            new UpdateRolePermissionsRequest
            {
                PermissionKeys = ["identity.users.read", "identity.users.write"]
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(deleteResult);
        Assert.IsType<NoContentResult>(updateResult);
        Assert.Equal(roleId, Assert.IsType<DeleteRoleCommand>(sender.Requests[0]).RoleId);
        UpdateRolePermissionsCommand updateCommand = Assert.IsType<UpdateRolePermissionsCommand>(sender.Requests[1]);
        Assert.Equal(["identity.users.read", "identity.users.write"], updateCommand.RolePermissionKeys);
    }

    [Fact]
    public async Task GetRolePermissions_AndMembersPage_MapResults()
    {
        Guid roleId = Guid.Parse("135ff0fd-f4e6-430c-a0ad-af6f6d69b5d9");
        var sender = new FakeSender();
        sender.Handle<GetRolePermissionsQuery, IReadOnlyCollection<string>>(_ =>
        [
            "identity.users.read",
            "identity.users.write"
        ]);
        sender.Handle<GetRoleMembersPageQuery, PagedResult<UserListItemResult>>(query =>
        {
            Assert.Equal(roleId, query.RoleId);
            Assert.Equal(3, query.Pagination.PageNumber);
            Assert.Equal(10, query.Pagination.PageSize);

            return new PagedResult<UserListItemResult>(
                items:
                [
                    new UserListItemResult
                    {
                        Id = Guid.Parse("56e81fd8-d1b5-4b0f-af76-0bf46dcfe4c6"),
                        AvatarUrl = "/avatars/neo.png",
                        Email = "neo@matrix.local",
                        Username = "neo",
                        IsEmailConfirmed = true,
                        IsLocked = false,
                        IsDeleted = false,
                        CreatedAtUtc = new DateTime(2048, 6, 1, 6, 0, 0, DateTimeKind.Utc),
                        LastVisitedAtUtc = new DateTime(2048, 6, 1, 12, 0, 0, DateTimeKind.Utc)
                    }
                ],
                totalCount: 21,
                pageNumber: 3,
                pageSize: 10);
        });
        var controller = new AdminRolesController(sender);

        ActionResult<RolePermissionsResponse> permissionsResult = await controller.GetRolePermissions(roleId, CancellationToken.None);
        ActionResult<PagedResult<UserListItemResponse>> membersResult = await controller.GetRoleMembersPage(
            roleId: roleId,
            pageNumber: 3,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        OkObjectResult permissionsOk = Assert.IsType<OkObjectResult>(permissionsResult.Result);
        RolePermissionsResponse permissions = Assert.IsType<RolePermissionsResponse>(permissionsOk.Value);
        Assert.Equal(["identity.users.read", "identity.users.write"], permissions.PermissionKeys);

        OkObjectResult membersOk = Assert.IsType<OkObjectResult>(membersResult.Result);
        PagedResult<UserListItemResponse> members = Assert.IsType<PagedResult<UserListItemResponse>>(membersOk.Value);
        UserListItemResponse member = Assert.Single(members.Items);
        Assert.Equal(21, members.TotalCount);
        Assert.Equal("neo", member.Username);
    }
}
