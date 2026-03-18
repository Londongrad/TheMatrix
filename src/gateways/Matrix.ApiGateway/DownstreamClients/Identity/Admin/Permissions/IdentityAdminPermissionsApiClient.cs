using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Admin.Permissions.Requests;
using Matrix.Identity.Contracts.Admin.Permissions.Responses;
using System.Net.Http.Json;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions
{
    public sealed class IdentityAdminPermissionsApiClient(HttpClient httpClient) : IIdentityAdminPermissionsClient
    {
        private const string ServiceName = DownstreamServiceNames.Identity;
        private const string PermissionsEndpoint = "/api/admin/permissions";
        private const string DefaultUserAccessEndpoint = "/api/admin/permissions/default-user-access";
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

        public async Task<DefaultUserAccessPermissionsResponse> GetDefaultUserAccessPermissionsAsync(
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage resp = await _httpClient.GetAsync(
                requestUri: DefaultUserAccessEndpoint,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<DefaultUserAccessPermissionsResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: DefaultUserAccessEndpoint);
        }

        public async Task UpdateDefaultUserAccessPermissionsAsync(
            UpdateDefaultUserAccessPermissionsRequest request,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage resp = await _httpClient.PutAsJsonAsync(
                requestUri: DefaultUserAccessEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }
    }
}
