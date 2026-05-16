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

namespace Matrix.ApiGateway.Tests.DownstreamClients.Identity;

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
                    CreatedAtUtc = new DateTime(2048, 6, 12, 10, 0, 0, DateTimeKind.Utc)
                }
            ],
            totalCount: 1,
            pageNumber: 3,
            pageSize: 15);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, page))
        };
        IIdentityAdminUsersClient client = CreateIdentityAdminUsersApiClient(CreateHttpClient(handler));

        PagedResult<UserListItemResponse> result = await client.GetUsersPageAsync(3, 15, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/admin/users?pageNumber=3&pageSize=15", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAdminUsersApiClientAssignUserRolesAsync_WhenCalled_PutsJsonPayload()
    {
        Guid userId = Guid.Parse("0ea5e3d6-12b0-4de7-ae01-3fd2a2441495");
        Guid roleId = Guid.Parse("5661631d-bda4-4f13-9ff6-4f665d25c72d");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NoContent))
        };
        IIdentityAdminUsersClient client = CreateIdentityAdminUsersApiClient(CreateHttpClient(handler));

        await client.AssignUserRolesAsync(
            userId,
            new AssignUserRolesRequest
            {
                RoleIds = [roleId]
            },
            CancellationToken.None);

        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith($"/api/admin/users/{userId}/roles", request.RequestUri, StringComparison.Ordinal);
        Assert.Contains(roleId.ToString(), request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAdminRolesApiClientCreateRoleAsync_WhenCalled_PostsAndReturnsRole()
    {
        RoleResponse response = new()
        {
            Id = Guid.Parse("84eebf18-a84b-4fac-a89b-99def83f43f0"),
            Name = "CityManager",
            IsSystem = false,
            CreatedAtUtc = new DateTime(2048, 6, 12, 11, 30, 0, DateTimeKind.Utc)
        };
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, response))
        };
        IIdentityAdminRolesClient client = CreateIdentityAdminRolesApiClient(CreateHttpClient(handler));

        RoleResponse result = await client.CreateRoleAsync(
            new CreateRoleRequest
            {
                Name = "CityManager"
            },
            CancellationToken.None);

        Assert.Equal("CityManager", result.Name);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/admin/roles", request.RequestUri, StringComparison.Ordinal);
        Assert.Contains("\"name\":\"CityManager\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAdminRolesApiClientGetRoleMembersPageAsync_WhenCalled_UsesExpectedQuery()
    {
        Guid roleId = Guid.Parse("2ec4e274-f834-4911-b8d3-570f9bf3c936");
        PagedResult<UserListItemResponse> page = new(
            items: [],
            totalCount: 0,
            pageNumber: 2,
            pageSize: 10);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, page))
        };
        IIdentityAdminRolesClient client = CreateIdentityAdminRolesApiClient(CreateHttpClient(handler));

        PagedResult<UserListItemResponse> result = await client.GetRoleMembersPageAsync(roleId, 2, 10, CancellationToken.None);

        Assert.Equal(2, result.PageNumber);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/admin/roles/{roleId}/users?pageNumber=2&pageSize=10", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAdminPermissionsApiClientGetDefaultUserAccessPermissionsAsync_WhenCalled_ReturnsPayload()
    {
        DefaultUserAccessPermissionsResponse response = new()
        {
            Version = 17,
            PermissionKeys = ["cities.view", "economy.summary"]
        };
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, response))
        };
        IIdentityAdminPermissionsClient client = CreateIdentityAdminPermissionsApiClient(CreateHttpClient(handler));

        DefaultUserAccessPermissionsResponse result = await client.GetDefaultUserAccessPermissionsAsync(CancellationToken.None);

        Assert.Equal(17, result.Version);
        Assert.Contains("economy.summary", result.PermissionKeys);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/admin/permissions/default-user-access", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAdminPermissionsApiClientUpdateDefaultUserAccessPermissionsAsync_WhenCalled_PutsJsonPayload()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse(HttpStatusCode.NoContent))
        };
        IIdentityAdminPermissionsClient client = CreateIdentityAdminPermissionsApiClient(CreateHttpClient(handler));

        await client.UpdateDefaultUserAccessPermissionsAsync(
            new UpdateDefaultUserAccessPermissionsRequest
            {
                PermissionKeys = ["cities.view", "population.read"]
            },
            CancellationToken.None);

        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/api/admin/permissions/default-user-access", request.RequestUri, StringComparison.Ordinal);
        Assert.Contains("\"permissionKeys\":[\"cities.view\",\"population.read\"]", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityInternalUsersClientGetPermissionsVersionAsync_WhenPayloadIsNull_ThrowsInvalidOperationException()
    {
        Guid userId = Guid.Parse("d6f6e8e6-5062-4f87-9c35-9ff5be74311d");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.OK, "null"))
        };
        IIdentityInternalUsersClient client = CreateIdentityInternalUsersApiClient(CreateHttpClient(handler));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetPermissionsVersionAsync(userId, CancellationToken.None));

        Assert.Contains("missing payload", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IdentityInternalUsersClientGetAuthContextAsync_WhenCalled_UsesExpectedUrlAndReturnsContext()
    {
        Guid userId = Guid.Parse("649a8d5b-84ef-48fb-a5cf-673805fc29c9");
        UserAuthContextResponse response = new(
            PermissionsVersion: 11,
            EffectivePermissions: ["cities.launch", "population.read"]);
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, response))
        };
        IIdentityInternalUsersClient client = CreateIdentityInternalUsersApiClient(CreateHttpClient(handler));

        UserAuthContextResponse result = await client.GetAuthContextAsync(userId, CancellationToken.None);

        Assert.Equal(11, result.PermissionsVersion);
        Assert.Contains("cities.launch", result.EffectivePermissions);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith($"/api/internal/users/{userId}/auth-context", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityInternalUsersClientGetDefaultUserAccessVersionAsync_WhenDownstreamFails_ThrowsDownstreamServiceException()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.ServiceUnavailable, "{\"error\":\"identity-unavailable\"}"))
        };
        IIdentityInternalUsersClient client = CreateIdentityInternalUsersApiClient(CreateHttpClient(handler));

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => client.GetDefaultUserAccessVersionAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Contains("identity-unavailable", exception.Body, StringComparison.Ordinal);
    }
}
