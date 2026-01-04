using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public sealed class IdentityPermissionsVersionClient : IIdentityPermissionsVersionClient
    {
        private const string ApiKeyHeaderName = "X-Internal-Key";
        private readonly HttpClient _httpClient;

        public IdentityPermissionsVersionClient(
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
                requestUri: $"api/internal/users/{userId}/permissions-version",
                cancellationToken: cancellationToken);

            response.EnsureSuccessStatusCode();

            PermissionsVersionResponse? payload = await response.Content
               .ReadFromJsonAsync<PermissionsVersionResponse>(cancellationToken: cancellationToken);

            if (payload is null)
                throw new InvalidOperationException("Identity internal response is missing payload.");

            return payload.Version;
        }

        private sealed record PermissionsVersionResponse(int Version);
    }
}
