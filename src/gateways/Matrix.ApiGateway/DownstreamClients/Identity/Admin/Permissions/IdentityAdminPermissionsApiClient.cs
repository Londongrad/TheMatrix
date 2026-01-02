using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions
{
    public sealed class IdentityAdminPermissionsApiClient(HttpClient httpClient) : IIdentityAdminPermissionsClient
    {
        private const string ServiceName = "Identity";
        private const string PermissionsEndpoint = "/api/admin/permissions";
        private readonly HttpClient _httpClient = httpClient;

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
