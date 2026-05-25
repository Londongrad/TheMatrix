using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Controllers.Identity.Admin;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Permissions.Requests;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.Identity
{
    public sealed class IdentityAdminControllersTests
    {
        private static readonly DateTime CreatedAtUtc = new(
            year: 2048,
            month: 6,
            day: 1,
            hour: 8,
            minute: 0,
            second: 0,
            kind: DateTimeKind.Utc);

        [Fact]
        public async Task AdminUsersControllerGetUsersPage_NormalizesAvatarUrls()
        {
            var usersClient = new RecordingIdentityAdminUsersClient
            {
                UsersPageResult = new PagedResult<UserListItemResponse>(
                    items:
                    [
                        new UserListItemResponse
                        {
                            Id = Guid.Parse("752e45ff-cfeb-445d-a493-293b84623926"),
                            AvatarUrl = "/avatars/mira.png",
                            Email = "mira@matrix.test",
                            Username = "mira",
                            IsEmailConfirmed = true,
                            IsLocked = false,
                            IsDeleted = false,
                            CreatedAtUtc = new DateTime(
                                year: 2048,
                                month: 6,
                                day: 1,
                                hour: 8,
                                minute: 0,
                                second: 0,
                                kind: DateTimeKind.Utc)
                        }
                    ],
                    totalCount: 1,
                    pageNumber: 2,
                    pageSize: 25)
            };
            var controller = new AdminUsersController(
                usersClient: usersClient,
                distributedCache: new RecordingDistributedCache())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = CreateHttpContext()
                }
            };

            ActionResult<PagedResult<UserListItemResponse>> actionResult = await controller.GetUsersPage(
                pageNumber: 2,
                pageSize: 25,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PagedResult<UserListItemResponse> page = Assert.IsType<PagedResult<UserListItemResponse>>(ok.Value);
            Assert.Equal(
                expected: "https://gateway.test/gw/avatars/mira.png",
                actual: Assert.Single(page.Items)
                   .AvatarUrl);
            Assert.Equal(
                expected: 2,
                actual: usersClient.LastPageNumber);
            Assert.Equal(
                expected: 25,
                actual: usersClient.LastPageSize);
        }

        [Fact]
        public async Task AdminUsersControllerRestoreUser_ClearsPermissionsVersionCache()
        {
            var userId = Guid.Parse("69853b9b-6135-4547-9fe7-cf9d17642ce7");
            var cache = new RecordingDistributedCache();
            cache.SeedString(
                key: AuthorizationCacheKeys.PermissionsVersion(userId),
                value: "5");
            cache.SeedString(
                key: AuthorizationCacheKeys.PermissionsVersionStale(userId),
                value: "5");
            var usersClient = new RecordingIdentityAdminUsersClient();
            var controller = new AdminUsersController(
                usersClient: usersClient,
                distributedCache: cache)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = CreateHttpContext()
                }
            };

            IActionResult actionResult = await controller.RestoreUser(
                userId: userId,
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(actionResult);
            Assert.Equal(
                expected: userId,
                actual: usersClient.LastRestoreUserId);
            Assert.Null(cache.ReadString(AuthorizationCacheKeys.PermissionsVersion(userId)));
            Assert.Null(cache.ReadString(AuthorizationCacheKeys.PermissionsVersionStale(userId)));
        }

        [Fact]
        public async Task AdminUsersControllerAssignUserRoles_ClearsPermissionsVersionCache()
        {
            var userId = Guid.Parse("a7f31d51-c223-470c-97f9-32a6c0372a38");
            var cache = new RecordingDistributedCache();
            cache.SeedString(
                key: AuthorizationCacheKeys.PermissionsVersion(userId),
                value: "7");
            cache.SeedString(
                key: AuthorizationCacheKeys.PermissionsVersionStale(userId),
                value: "7");
            AssignUserRolesRequest request = new()
            {
                RoleIds = [Guid.Parse("8c27dc5e-a7ac-4678-b092-0c868a1f166f")]
            };
            var usersClient = new RecordingIdentityAdminUsersClient();
            var controller = new AdminUsersController(
                usersClient: usersClient,
                distributedCache: cache)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = CreateHttpContext()
                }
            };

            IActionResult actionResult = await controller.AssignUserRoles(
                userId: userId,
                request: request,
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(actionResult);
            Assert.Same(
                expected: request,
                actual: usersClient.LastAssignRolesRequest);
            Assert.Null(cache.ReadString(AuthorizationCacheKeys.PermissionsVersion(userId)));
            Assert.Null(cache.ReadString(AuthorizationCacheKeys.PermissionsVersionStale(userId)));
        }

        [Fact]
        public async Task AdminRolesControllerGetRoleMembersPage_ReturnsOk()
        {
            var roleId = Guid.Parse("a577a8c1-6978-4680-95bd-6d82e0d2b7a9");
            var rolesClient = new RecordingIdentityAdminRolesClient
            {
                RoleMembersResult = new PagedResult<UserListItemResponse>(
                    items: [],
                    totalCount: 0,
                    pageNumber: 3,
                    pageSize: 10)
            };
            var controller = new AdminRolesController(rolesClient);

            ActionResult<PagedResult<UserListItemResponse>> actionResult = await controller.GetRoleMembersPage(
                roleId: roleId,
                pageNumber: 3,
                pageSize: 10,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PagedResult<UserListItemResponse> page = Assert.IsType<PagedResult<UserListItemResponse>>(ok.Value);
            Assert.Equal(
                expected: 3,
                actual: page.PageNumber);
            Assert.Equal(
                expected: roleId,
                actual: rolesClient.LastRoleMembersRoleId);
        }

        [Fact]
        public async Task AdminPermissionsControllerUpdateDefaultUserAccessPermissions_ReturnsNoContent()
        {
            UpdateDefaultUserAccessPermissionsRequest request = new()
            {
                PermissionKeys =
                [
                    "cities.view",
                    "economy.summary"
                ]
            };
            var permissionsClient = new RecordingIdentityAdminPermissionsClient();
            var controller = new AdminPermissionsController(permissionsClient);

            IActionResult actionResult = await controller.UpdateDefaultUserAccessPermissions(
                request: request,
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(actionResult);
            Assert.Same(
                expected: request,
                actual: permissionsClient.LastUpdateRequest);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            DefaultHttpContext httpContext = new();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("gateway.test");
            httpContext.Request.PathBase = new PathString("/gw");
            return httpContext;
        }

        private sealed class RecordingIdentityAdminUsersClient : IIdentityAdminUsersClient
        {
            public PagedResult<UserListItemResponse>? UsersPageResult { get; set; }
            public int? LastPageNumber { get; private set; }
            public int? LastPageSize { get; private set; }
            public Guid? LastRestoreUserId { get; private set; }
            public AssignUserRolesRequest? LastAssignRolesRequest { get; private set; }

            public Task<PagedResult<UserListItemResponse>> GetUsersPageAsync(
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken)
            {
                LastPageNumber = pageNumber;
                LastPageSize = pageSize;
                return Task.FromResult(
                    UsersPageResult ??
                    new PagedResult<UserListItemResponse>(
                        items: [],
                        totalCount: 0,
                        pageNumber: pageNumber,
                        pageSize: pageSize));
            }

            public Task<UserDetailsResponse> GetUserDetailsAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    new UserDetailsResponse
                    {
                        Id = userId,
                        Email = "mira@matrix.test",
                        Username = "mira",
                        CreatedAtUtc = CreatedAtUtc
                    });
            }

            public Task LockUserAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task UnlockUserAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task RestoreUserAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                LastRestoreUserId = userId;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyCollection<UserRoleResponse>> GetUserRolesAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyCollection<UserRoleResponse>>([]);
            }

            public Task AssignUserRolesAsync(
                Guid userId,
                AssignUserRolesRequest request,
                CancellationToken cancellationToken)
            {
                LastAssignRolesRequest = request;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyCollection<UserPermissionResponse>> GetUserPermissionsAsync(
                Guid userId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyCollection<UserPermissionResponse>>([]);
            }

            public Task UpdateUserPermissionsAsync(
                Guid userId,
                UpdateUserPermissionsRequest request,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task GrantUserPermissionAsync(
                Guid userId,
                UserPermissionRequest request,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DepriveUserPermissionAsync(
                Guid userId,
                UserPermissionRequest request,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingIdentityAdminRolesClient : IIdentityAdminRolesClient
        {
            public PagedResult<UserListItemResponse>? RoleMembersResult { get; set; }
            public Guid? LastRoleMembersRoleId { get; private set; }

            public Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyCollection<RoleResponse>>([]);
            }

            public Task<RoleResponse> CreateRoleAsync(
                CreateRoleRequest request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    new RoleResponse
                    {
                        Id = Guid.NewGuid(),
                        Name = request.Name,
                        CreatedAtUtc = CreatedAtUtc
                    });
            }

            public Task<RoleResponse> RenameRoleAsync(
                Guid roleId,
                RenameRoleRequest request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(
                    new RoleResponse
                    {
                        Id = roleId,
                        Name = request.Name,
                        CreatedAtUtc = CreatedAtUtc
                    });
            }

            public Task DeleteRoleAsync(
                Guid roleId,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<RolePermissionsResponse> GetRolePermissionsAsync(
                Guid roleId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new RolePermissionsResponse());
            }

            public Task UpdateRolePermissionsAsync(
                Guid roleId,
                UpdateRolePermissionsRequest request,
                CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task<PagedResult<UserListItemResponse>> GetRoleMembersPageAsync(
                Guid roleId,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken)
            {
                LastRoleMembersRoleId = roleId;
                return Task.FromResult(
                    RoleMembersResult ??
                    new PagedResult<UserListItemResponse>(
                        items: [],
                        totalCount: 0,
                        pageNumber: pageNumber,
                        pageSize: pageSize));
            }
        }

        private sealed class RecordingIdentityAdminPermissionsClient : IIdentityAdminPermissionsClient
        {
            public UpdateDefaultUserAccessPermissionsRequest? LastUpdateRequest { get; private set; }

            public Task<IReadOnlyCollection<PermissionCatalogItemResponse>> GetPermissionsAsync(
                CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyCollection<PermissionCatalogItemResponse>>([]);
            }

            public Task<DefaultUserAccessPermissionsResponse> GetDefaultUserAccessPermissionsAsync(
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new DefaultUserAccessPermissionsResponse());
            }

            public Task UpdateDefaultUserAccessPermissionsAsync(
                UpdateDefaultUserAccessPermissionsRequest request,
                CancellationToken cancellationToken)
            {
                LastUpdateRequest = request;
                return Task.CompletedTask;
            }
        }
    }
}
