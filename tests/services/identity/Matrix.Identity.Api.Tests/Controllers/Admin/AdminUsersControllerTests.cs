using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Api.Controllers.Admin;
using Matrix.Identity.Application.UseCases.Admin.Users.DepriveUserPermission;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Application.UseCases.Admin.Users.GrantUserPermission;
using Matrix.Identity.Application.UseCases.Admin.Users.LockUser;
using Matrix.Identity.Application.UseCases.Admin.Users.RestoreUser;
using Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserPermissions;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Matrix.Identity.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Admin
{
    public sealed class AdminUsersControllerTests
    {
        [Fact]
        public async Task GetUsersPage_MapsPagedResult()
        {
            var sender = new FakeSender();
            sender.Handle<GetUsersPageQuery, PagedResult<UserListItemResult>>(query =>
            {
                Assert.Equal(
                    expected: 2,
                    actual: query.Pagination.PageNumber);
                Assert.Equal(
                    expected: 25,
                    actual: query.Pagination.PageSize);

                return new PagedResult<UserListItemResult>(
                    items:
                    [
                        new UserListItemResult
                        {
                            Id = Guid.Parse("f1f1da9d-b6aa-4bbd-9720-3e5dca61adf8"),
                            AvatarUrl = "/avatars/neo.png",
                            Email = "neo@matrix.local",
                            Username = "neo",
                            IsEmailConfirmed = true,
                            IsLocked = false,
                            IsDeleted = false,
                            CreatedAtUtc = new DateTime(
                                year: 2048,
                                month: 6,
                                day: 1,
                                hour: 7,
                                minute: 0,
                                second: 0,
                                kind: DateTimeKind.Utc),
                            LastVisitedAtUtc = new DateTime(
                                year: 2048,
                                month: 6,
                                day: 1,
                                hour: 11,
                                minute: 0,
                                second: 0,
                                kind: DateTimeKind.Utc)
                        }
                    ],
                    totalCount: 41,
                    pageNumber: 2,
                    pageSize: 25);
            });
            var controller = new AdminUsersController(sender);

            ActionResult<PagedResult<UserListItemResponse>> actionResult = await controller.GetUsersPage(
                pageNumber: 2,
                pageSize: 25,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PagedResult<UserListItemResponse> response = Assert.IsType<PagedResult<UserListItemResponse>>(ok.Value);

            UserListItemResponse item = Assert.Single(response.Items);
            Assert.Equal(
                expected: 41,
                actual: response.TotalCount);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: item.Email);
            Assert.True(item.IsEmailConfirmed);
        }

        [Fact]
        public async Task GetUserDetails_MapsResponse()
        {
            var userId = Guid.Parse("ab481d1a-bf9b-4a06-b97a-a08f59af12b3");
            var sender = new FakeSender();
            sender.Handle<GetUserDetailsQuery, UserDetailsResult>(_ => new UserDetailsResult
            {
                Id = userId,
                AvatarUrl = "/avatars/neo.png",
                Username = "neo",
                Email = "neo@matrix.local",
                IsEmailConfirmed = true,
                IsLocked = false,
                IsDeleted = false,
                PermissionsVersion = 18,
                CreatedAtUtc = new DateTime(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 7,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc),
                LastVisitedAtUtc = new DateTime(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc)
            });
            var controller = new AdminUsersController(sender);

            ActionResult<UserDetailsResponse> actionResult = await controller.GetUserDetails(
                userId: userId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            UserDetailsResponse response = Assert.IsType<UserDetailsResponse>(ok.Value);

            Assert.Equal(
                expected: userId,
                actual: response.Id);
            Assert.Equal(
                expected: "neo",
                actual: response.Username);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: response.Email);
            Assert.Equal(
                expected: 18,
                actual: response.PermissionsVersion);
        }

        [Fact]
        public async Task ManageUserState_UsesMatchingCommands()
        {
            var userId = Guid.Parse("fca744a9-776c-4c13-b558-18e8ae17ddbe");
            var sender = new FakeSender();
            sender.Handle<LockUserCommand>(_ => { });
            sender.Handle<UnlockUserCommand>(_ => { });
            sender.Handle<RestoreUserCommand>(_ => { });
            var controller = new AdminUsersController(sender);

            IActionResult lockResult = await controller.LockUser(
                userId: userId,
                cancellationToken: CancellationToken.None);
            IActionResult unlockResult = await controller.UnlockUser(
                userId: userId,
                cancellationToken: CancellationToken.None);
            IActionResult restoreResult = await controller.RestoreUser(
                userId: userId,
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(lockResult);
            Assert.IsType<NoContentResult>(unlockResult);
            Assert.IsType<NoContentResult>(restoreResult);
            Assert.Collection(
                collection: sender.Requests,
                request => Assert.Equal(
                    expected: userId,
                    actual: Assert.IsType<LockUserCommand>(request)
                       .UserId),
                request => Assert.Equal(
                    expected: userId,
                    actual: Assert.IsType<UnlockUserCommand>(request)
                       .UserId),
                request => Assert.Equal(
                    expected: userId,
                    actual: Assert.IsType<RestoreUserCommand>(request)
                       .UserId));
        }

        [Fact]
        public async Task GetUserRoles_MapsRoleItems()
        {
            var userId = Guid.Parse("b2d2e64e-9c84-4536-b6a5-5baf9b742f32");
            var sender = new FakeSender();
            sender.Handle<GetUserRolesQuery, IReadOnlyCollection<UserRoleResult>>(_ =>
            [
                new UserRoleResult
                {
                    Id = Guid.Parse("5bc8a019-4641-445c-b3ef-51b67076c905"),
                    Name = "Operator",
                    IsSystem = false,
                    CreatedAtUtc = new DateTime(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 6,
                        minute: 0,
                        second: 0,
                        kind: DateTimeKind.Utc)
                }
            ]);
            var controller = new AdminUsersController(sender);

            ActionResult<IReadOnlyCollection<UserRoleResponse>> actionResult = await controller.GetUserRoles(
                userId: userId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            IReadOnlyCollection<UserRoleResponse> response =
                Assert.IsAssignableFrom<IReadOnlyCollection<UserRoleResponse>>(ok.Value);

            Assert.Single(response);
            Assert.Equal(
                expected: "Operator",
                actual: response.Single()
                   .Name);
        }

        [Fact]
        public async Task UpdateUserRoles_UsesRequestRoleIds()
        {
            var userId = Guid.Parse("f11176f0-06ca-46bb-815f-2f31808ed760");
            var roleId = Guid.Parse("cae551ef-0805-46be-91cb-f734818af225");
            var sender = new FakeSender();
            sender.Handle<UpdateUserRolesCommand>(_ => { });
            var controller = new AdminUsersController(sender);

            IActionResult result = await controller.AssignUserRoles(
                userId: userId,
                request: new AssignUserRolesRequest
                {
                    RoleIds = [roleId]
                },
                cancellationToken: CancellationToken.None);

            UpdateUserRolesCommand command = Assert.IsType<UpdateUserRolesCommand>(sender.Requests.Single());

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(
                expected: userId,
                actual: command.UserId);
            Assert.Equal(
                expected: [roleId],
                actual: command.RoleIds);
        }

        [Fact]
        public async Task ManageUserPermissions_MapsResponsesAndCommands()
        {
            var userId = Guid.Parse("59e16222-ccbd-48de-b8fc-c8ad21597013");
            var sender = new FakeSender();
            sender.Handle<GetUserPermissionsQuery, IReadOnlyCollection<UserPermissionOverrideResult>>(_ =>
            [
                new UserPermissionOverrideResult
                {
                    PermissionKey = "identity.users.read",
                    Effect = PermissionEffect.Allow
                },
                new UserPermissionOverrideResult
                {
                    PermissionKey = "identity.users.delete",
                    Effect = PermissionEffect.Deny
                }
            ]);
            sender.Handle<GrantUserPermissionCommand>(_ => { });
            sender.Handle<UpdateUserPermissionsCommand>(_ => { });
            sender.Handle<DepriveUserPermissionCommand>(_ => { });
            var controller = new AdminUsersController(sender);

            ActionResult<IReadOnlyCollection<UserPermissionResponse>> getResult =
                await controller.GetUserPermissions(
                    userId: userId,
                    cancellationToken: CancellationToken.None);
            IActionResult grantResult = await controller.GrantUserPermission(
                userId: userId,
                request: new UserPermissionRequest
                {
                    PermissionKey = "identity.users.read"
                },
                cancellationToken: CancellationToken.None);
            IActionResult updateResult = await controller.UpdateUserPermissions(
                userId: userId,
                request: new UpdateUserPermissionsRequest
                {
                    Overrides =
                    [
                        new UserPermissionOverrideRequest
                        {
                            PermissionKey = "identity.users.delete",
                            Effect = "Deny"
                        }
                    ]
                },
                cancellationToken: CancellationToken.None);
            IActionResult depriveResult = await controller.DepriveUserPermission(
                userId: userId,
                request: new UserPermissionRequest
                {
                    PermissionKey = "identity.users.write"
                },
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(getResult.Result);
            IReadOnlyCollection<UserPermissionResponse> response =
                Assert.IsAssignableFrom<IReadOnlyCollection<UserPermissionResponse>>(ok.Value);
            Assert.Collection(
                collection: response,
                item =>
                {
                    Assert.Equal(
                        expected: "identity.users.read",
                        actual: item.PermissionKey);
                    Assert.Equal(
                        expected: "Allow",
                        actual: item.Effect);
                },
                item =>
                {
                    Assert.Equal(
                        expected: "identity.users.delete",
                        actual: item.PermissionKey);
                    Assert.Equal(
                        expected: "Deny",
                        actual: item.Effect);
                });

            Assert.IsType<NoContentResult>(grantResult);
            Assert.IsType<NoContentResult>(updateResult);
            Assert.IsType<NoContentResult>(depriveResult);
            Assert.Equal(
                expected: "identity.users.read",
                actual: Assert.IsType<GrantUserPermissionCommand>(sender.Requests[1])
                   .TargetPermissionKey);
            UpdateUserPermissionsCommand
                updateCommand = Assert.IsType<UpdateUserPermissionsCommand>(sender.Requests[2]);
            Assert.Equal(
                expected: "identity.users.delete",
                actual: updateCommand.Overrides.Single()
                   .PermissionKey);
            Assert.Equal(
                expected: "Deny",
                actual: updateCommand.Overrides.Single()
                   .Effect);
            Assert.Equal(
                expected: "identity.users.write",
                actual: Assert.IsType<DepriveUserPermissionCommand>(sender.Requests[3])
                   .TargetPermissionKey);
        }
    }
}
