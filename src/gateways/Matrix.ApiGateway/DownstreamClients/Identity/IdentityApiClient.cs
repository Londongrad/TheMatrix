using Matrix.ApiGateway.DownstreamClients.Identity.Contracts;

namespace Matrix.ApiGateway.DownstreamClients.Identity
{
    public sealed class IdentityApiClient(HttpClient httpClient) : IIdentityApiClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private const string RegisterEndpoint = "api/auth/register";
        private const string LoginEndpoint = "api/auth/login";

        private const string LogoutEndpoint = "api/auth/logout";
        private const string RefreshEndpoint = "api/auth/refresh";

        public async Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            return await _httpClient.PostAsJsonAsync(RegisterEndpoint, request, ct);
        }

        public async Task<HttpResponseMessage> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            return await _httpClient.PostAsJsonAsync(LoginEndpoint, request, ct);
        }

        public async Task<HttpResponseMessage> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
        {
            return await _httpClient.PostAsJsonAsync(RefreshEndpoint, request, ct);
        }

        public async Task LogoutAsync(RefreshRequest request, CancellationToken ct = default)
        {
            await _httpClient.PostAsJsonAsync(LogoutEndpoint, request, ct);
        }
    }
}