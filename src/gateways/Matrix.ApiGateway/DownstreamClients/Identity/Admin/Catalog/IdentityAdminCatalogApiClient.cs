using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using Matrix.Identity.Contracts.Admin.Roles.Responses;

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
