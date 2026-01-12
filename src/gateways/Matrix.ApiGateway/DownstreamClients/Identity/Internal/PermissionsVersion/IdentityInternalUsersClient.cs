using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion
{
    public sealed class IdentityInternalUsersClient : IIdentityInternalUsersClient
    {
        private const string ServiceName = "Identity";
        private const string ApiKeyHeaderName = "X-Internal-Key";
        private const string PermissionsVersionEndpoint = "api/internal/users/{0}/permissions-version";
        private readonly HttpClient _httpClient;

        public IdentityInternalUsersClient(
            HttpClient httpClient,
            IOptions<IdentityInternalOptions> options)
        {
            _httpClient = httpClient;

            if (!httpClient.DefaultRequestHeaders.Contains(ApiKeyHeaderName))
                httpClient.DefaultRequestHeaders.Add(
                    name: ApiKeyHeaderName,
                    value: options.Value.ApiKey);
        }

        public async Task<int> GetPermissionsVersionAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                requestUri: string.Format(
                    format: PermissionsVersionEndpoint,
                    arg0: userId),
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);

            PermissionsVersionResponse? payload = await response.Content
               .ReadFromJsonAsync<PermissionsVersionResponse>(cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("Identity internal response is missing payload.");

            return payload.Version;
        }
    }
}
