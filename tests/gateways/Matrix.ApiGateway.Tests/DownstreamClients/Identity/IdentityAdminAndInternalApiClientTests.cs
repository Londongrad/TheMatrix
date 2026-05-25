using System.Net;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Permissions.Requests;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Matrix.Identity.Contracts.Internal.Responses;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Identity
{
    public sealed class IdentityAdminAndInternalApiClientTests
    {
        [Fact]
        public async Task IdentityAdminUsersApiClientGetUsersPageAsync_WhenCalled_UsesPaginationQuery()
        {
            PagedResult<UserListItemResponse> page = new(
                items:
                [
                    new UserListItemResponse
                    {
                        Id = Guid.Parse("63cf0f04-a48d-4c71-a947-64ebfc948919"),
                        Email = "mira@matrix.test",
                        Username = "mira",
                        IsEmailConfirmed = true,
                        IsLocked = false,
                        IsDeleted = false,
                        CreatedAtUtc = new DateTime(
                            year: 2048,
                            month: 6,
                            day: 12,
                            hour: 10,
                            minute: 0,
                            second: 0,
                            kind: DateTimeKind.Utc)
                    }
                ],
                totalCount: 1,
                pageNumber: 3,
                pageSize: 15);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: page))
            };
            IIdentityAdminUsersClient client = CreateIdentityAdminUsersApiClient(CreateHttpClient(handler));

            PagedResult<UserListItemResponse> result = await client.GetUsersPageAsync(
                pageNumber: 3,
                pageSize: 15,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: result.TotalCount);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: "/api/admin/users?pageNumber=3&pageSize=15",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAdminUsersApiClientAssignUserRolesAsync_WhenCalled_PutsJsonPayload()
        {
            var userId = Guid.Parse("0ea5e3d6-12b0-4de7-ae01-3fd2a2441495");
            var roleId = Guid.Parse("5661631d-bda4-4f13-9ff6-4f665d25c72d");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NoContent))
            };
            IIdentityAdminUsersClient client = CreateIdentityAdminUsersApiClient(CreateHttpClient(handler));

            await client.AssignUserRolesAsync(
                userId: userId,
                request: new AssignUserRolesRequest
                {
                    RoleIds = [roleId]
                },
                cancellationToken: CancellationToken.None);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Put,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: $"/api/admin/users/{userId}/roles",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: roleId.ToString(),
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAdminRolesApiClientCreateRoleAsync_WhenCalled_PostsAndReturnsRole()
        {
            RoleResponse response = new()
            {
                Id = Guid.Parse("84eebf18-a84b-4fac-a89b-99def83f43f0"),
                Name = "CityManager",
                IsSystem = false,
                CreatedAtUtc = new DateTime(
                    year: 2048,
                    month: 6,
                    day: 12,
                    hour: 11,
                    minute: 30,
                    second: 0,
                    kind: DateTimeKind.Utc)
            };
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
            IIdentityAdminRolesClient client = CreateIdentityAdminRolesApiClient(CreateHttpClient(handler));

            RoleResponse result = await client.CreateRoleAsync(
                request: new CreateRoleRequest
                {
                    Name = "CityManager"
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "CityManager",
                actual: result.Name);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: "/api/admin/roles",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"name\":\"CityManager\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAdminRolesApiClientGetRoleMembersPageAsync_WhenCalled_UsesExpectedQuery()
        {
            var roleId = Guid.Parse("2ec4e274-f834-4911-b8d3-570f9bf3c936");
            PagedResult<UserListItemResponse> page = new(
                items: [],
                totalCount: 0,
                pageNumber: 2,
                pageSize: 10);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: page))
            };
            IIdentityAdminRolesClient client = CreateIdentityAdminRolesApiClient(CreateHttpClient(handler));

            PagedResult<UserListItemResponse> result = await client.GetRoleMembersPageAsync(
                roleId: roleId,
                pageNumber: 2,
                pageSize: 10,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: result.PageNumber);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: $"/api/admin/roles/{roleId}/users?pageNumber=2&pageSize=10",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            IdentityAdminPermissionsApiClientGetDefaultUserAccessPermissionsAsync_WhenCalled_ReturnsPayload()
        {
            DefaultUserAccessPermissionsResponse response = new()
            {
                Version = 17,
                PermissionKeys =
                [
                    "cities.view",
                    "economy.summary"
                ]
            };
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
            IIdentityAdminPermissionsClient client = CreateIdentityAdminPermissionsApiClient(CreateHttpClient(handler));

            DefaultUserAccessPermissionsResponse result =
                await client.GetDefaultUserAccessPermissionsAsync(CancellationToken.None);

            Assert.Equal(
                expected: 17,
                actual: result.Version);
            Assert.Contains(
                expected: "economy.summary",
                collection: result.PermissionKeys);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: "/api/admin/permissions/default-user-access",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            IdentityAdminPermissionsApiClientUpdateDefaultUserAccessPermissionsAsync_WhenCalled_PutsJsonPayload()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NoContent))
            };
            IIdentityAdminPermissionsClient client = CreateIdentityAdminPermissionsApiClient(CreateHttpClient(handler));

            await client.UpdateDefaultUserAccessPermissionsAsync(
                request: new UpdateDefaultUserAccessPermissionsRequest
                {
                    PermissionKeys =
                    [
                        "cities.view",
                        "population.read"
                    ]
                },
                cancellationToken: CancellationToken.None);

            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Put,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: "/api/admin/permissions/default-user-access",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"permissionKeys\":[\"cities.view\",\"population.read\"]",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            IdentityInternalUsersClientGetPermissionsVersionAsync_WhenPayloadIsNull_ThrowsInvalidOperationException()
        {
            var userId = Guid.Parse("d6f6e8e6-5062-4f87-9c35-9ff5be74311d");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateStringResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: "null"))
            };
            IIdentityInternalUsersClient client = CreateIdentityInternalUsersApiClient(CreateHttpClient(handler));

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => client.GetPermissionsVersionAsync(
                    userId: userId,
                    cancellationToken: CancellationToken.None));

            Assert.Contains(
                expectedSubstring: "missing payload",
                actualString: exception.Message,
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task IdentityInternalUsersClientGetAuthContextAsync_WhenCalled_UsesExpectedUrlAndReturnsContext()
        {
            var userId = Guid.Parse("649a8d5b-84ef-48fb-a5cf-673805fc29c9");
            UserAuthContextResponse response = new(
                PermissionsVersion: 11,
                EffectivePermissions:
                [
                    "cities.launch",
                    "population.read"
                ]);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
            IIdentityInternalUsersClient client = CreateIdentityInternalUsersApiClient(CreateHttpClient(handler));

            UserAuthContextResponse result = await client.GetAuthContextAsync(
                userId: userId,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 11,
                actual: result.PermissionsVersion);
            Assert.Contains(
                expected: "cities.launch",
                collection: result.EffectivePermissions);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: $"/api/internal/users/{userId}/auth-context",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            IdentityInternalUsersClientGetDefaultUserAccessVersionAsync_WhenDownstreamFails_ThrowsDownstreamServiceException()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateStringResponse(
                        statusCode: HttpStatusCode.ServiceUnavailable,
                        payload: "{\"error\":\"identity-unavailable\"}"))
            };
            IIdentityInternalUsersClient client = CreateIdentityInternalUsersApiClient(CreateHttpClient(handler));

            DownstreamServiceException exception =
                await Assert.ThrowsAsync<DownstreamServiceException>(()
                    => client.GetDefaultUserAccessVersionAsync(CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.ServiceUnavailable,
                actual: exception.StatusCode);
            Assert.Contains(
                expectedSubstring: "identity-unavailable",
                actualString: exception.Body,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
