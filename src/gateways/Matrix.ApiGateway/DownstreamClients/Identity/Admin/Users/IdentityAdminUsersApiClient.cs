using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Users.Requests;
using Matrix.Identity.Contracts.Admin.Users.Responses;
using Microsoft.AspNetCore.WebUtilities;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users
{
    public sealed class IdentityAdminUsersApiClient(HttpClient httpClient) : IIdentityAdminUsersClient
    {
        private const string ServiceName = "Identity";
        private const string UsersEndpoint = "/api/admin/users";
        private readonly HttpClient _httpClient = httpClient;

        public async Task<PagedResult<UserListItemResponse>> GetUsersPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken ct)
        {
            string url = QueryHelpers.AddQueryString(
                uri: UsersEndpoint,
                queryString: new Dictionary<string, string?>
                {
                    ["pageNumber"] = pageNumber.ToString(),
                    ["pageSize"] = pageSize.ToString()
                });

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: ct);

            return await resp.ReadJsonOrThrowDownstreamAsync<PagedResult<UserListItemResponse>>(
                serviceName: ServiceName,
                cancellationToken: ct,
                requestUrl: url);
        }

        public async Task<UserDetailsResponse> GetUserDetailsAsync(
            Guid userId,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}";

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: ct);

            return await resp.ReadJsonOrThrowDownstreamAsync<UserDetailsResponse>(
                serviceName: ServiceName,
                cancellationToken: ct,
                requestUrl: url);
        }

        public async Task LockUserAsync(
            Guid userId,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/lock";

            using HttpResponseMessage resp = await _httpClient.PostAsync(
                requestUri: url,
                content: null,
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: ct);
        }

        public async Task UnlockUserAsync(
            Guid userId,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/unlock";

            using HttpResponseMessage resp = await _httpClient.PostAsync(
                requestUri: url,
                content: null,
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: ct);
        }

        public async Task<IReadOnlyCollection<UserRoleResponse>> GetUserRolesAsync(
            Guid userId,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/roles";

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: ct);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<UserRoleResponse>>(
                serviceName: ServiceName,
                cancellationToken: ct,
                requestUrl: url);
        }

        public async Task AssignUserRolesAsync(
            Guid userId,
            AssignUserRolesRequest request,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/roles";

            using HttpResponseMessage resp = await _httpClient.PutAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: ct);
        }

        public async Task<IReadOnlyCollection<UserPermissionResponse>> GetUserPermissionsAsync(
            Guid userId,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/permissions";

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: ct);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<UserPermissionResponse>>(
                serviceName: ServiceName,
                cancellationToken: ct,
                requestUrl: url);
        }

        public async Task GrantUserPermissionAsync(
            Guid userId,
            UserPermissionRequest request,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/permissions/grant";

            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: ct);
        }

        public async Task DepriveUserPermissionAsync(
            Guid userId,
            UserPermissionRequest request,
            CancellationToken ct)
        {
            string url = $"{UsersEndpoint}/{userId:guid}/permissions/deprive";

            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: ct);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: ct);
        }
    }
}
