using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public sealed class IdentityAuthApiClient(HttpClient httpClient) : IIdentityAuthClient
    {
        private const string RegisterEndpoint = "api/auth/register";
        private const string LoginEndpoint = "api/auth/login";

        private const string LogoutEndpoint = "api/auth/logout";
        private const string RefreshEndpoint = "api/auth/refresh";

        private const string SessionsEndpoint = "api/auth/sessions";
        private readonly HttpClient _httpClient = httpClient;

        public async Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken ct = default) =>
            await _httpClient.PostAsJsonAsync(requestUri: RegisterEndpoint, value: request, cancellationToken: ct);

        public async Task<HttpResponseMessage> LoginAsync(LoginRequest request, CancellationToken ct = default) =>
            await _httpClient.PostAsJsonAsync(requestUri: LoginEndpoint, value: request, cancellationToken: ct);

        public async Task<HttpResponseMessage> RefreshAsync(RefreshRequest request, CancellationToken ct = default) =>
            await _httpClient.PostAsJsonAsync(requestUri: RefreshEndpoint, value: request, cancellationToken: ct);

        public async Task LogoutAsync(RefreshRequest request, CancellationToken ct = default) =>
            await _httpClient.PostAsJsonAsync(requestUri: LogoutEndpoint, value: request, cancellationToken: ct);

        public async Task<HttpResponseMessage> GetSessionsAsync(string authorizationHeader,
            CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(method: HttpMethod.Get, requestUri: SessionsEndpoint);
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                request.Headers.TryAddWithoutValidation(name: "Authorization", value: authorizationHeader);

            return await _httpClient.SendAsync(request: request, cancellationToken: ct);
        }

        public async Task<HttpResponseMessage> RevokeSessionAsync(string authorizationHeader, Guid sessionId,
            CancellationToken ct = default)
        {
            using var request =
                new HttpRequestMessage(method: HttpMethod.Delete, requestUri: $"{SessionsEndpoint}/{sessionId}");
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                request.Headers.TryAddWithoutValidation(name: "Authorization", value: authorizationHeader);

            return await _httpClient.SendAsync(request: request, cancellationToken: ct);
        }

        public async Task<HttpResponseMessage> RevokeAllSessionsAsync(string authorizationHeader,
            CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(method: HttpMethod.Delete, requestUri: SessionsEndpoint);
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                request.Headers.TryAddWithoutValidation(name: "Authorization", value: authorizationHeader);

            return await _httpClient.SendAsync(request: request, cancellationToken: ct);
        }
    }
}
