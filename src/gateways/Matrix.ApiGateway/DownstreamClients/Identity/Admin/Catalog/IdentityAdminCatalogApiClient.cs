using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Requests;
using Matrix.Identity.Contracts.Admin.Roles.Responses;
using Matrix.Identity.Contracts.Admin.Users.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Catalog
{
    public sealed class IdentityAdminCatalogApiClient(HttpClient httpClient) : IIdentityAdminCatalogClient
    {
        private const string ServiceName = "Identity";
        private const string AdminBase = "/api/admin";
        private const string RolesEndpoint = AdminBase + "/roles";
        private const string PermissionsEndpoint = AdminBase + "/permissions";
        private readonly HttpClient _httpClient = httpClient;

        public async Task<IReadOnlyCollection<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: RolesEndpoint,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<RoleResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: RolesEndpoint);
        }

        public async Task<RoleResponse> CreateRoleAsync(
            CreateRoleRequest request,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: RolesEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<RoleResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: RolesEndpoint);
        }

        public async Task<RolePermissionsResponse> GetRolePermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            string url = $"{RolesEndpoint}/{roleId:D}/permissions";

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<RolePermissionsResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task UpdateRolePermissionsAsync(
            Guid roleId,
            UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken)
        {
            string url = $"{RolesEndpoint}/{roleId:D}/permissions";

            using HttpResponseMessage resp = await _httpClient.PutAsJsonAsync(
                requestUri: url,
                value: request,
                cancellationToken: cancellationToken);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        public async Task<PagedResult<UserListItemResponse>> GetRoleMembersPageAsync(
            Guid roleId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            string url = $"{RolesEndpoint}/{roleId:D}/users?pageNumber={pageNumber}&pageSize={pageSize}";

            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<PagedResult<UserListItemResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        public async Task<IReadOnlyCollection<PermissionCatalogItemResponse>> GetPermissionsAsync(
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: PermissionsEndpoint,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<PermissionCatalogItemResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: PermissionsEndpoint);
        }
    }
}
