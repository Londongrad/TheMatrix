using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public sealed class IdentityAuthApiClient(HttpClient httpClient) : IIdentityAuthClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private const string RegisterEndpoint = "api/auth/register";
        private const string LoginEndpoint = "api/auth/login";

        private const string LogoutEndpoint = "api/auth/logout";
        private const string RefreshEndpoint = "api/auth/refresh";

        private const string SessionsEndpoint = "api/auth/sessions";

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

        public async Task<HttpResponseMessage> GetSessionsAsync(string authorizationHeader, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, SessionsEndpoint);
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            return await _httpClient.SendAsync(request, ct);
        }

        public async Task<HttpResponseMessage> RevokeSessionAsync(string authorizationHeader, Guid sessionId, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{SessionsEndpoint}/{sessionId}");
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            return await _httpClient.SendAsync(request, ct);
        }

        public async Task<HttpResponseMessage> RevokeAllSessionsAsync(string authorizationHeader, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, SessionsEndpoint);
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            return await _httpClient.SendAsync(request, ct);
        }
    }
}