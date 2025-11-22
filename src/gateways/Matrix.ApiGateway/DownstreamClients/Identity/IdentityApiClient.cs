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

        public Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            return _httpClient.PostAsJsonAsync(RegisterEndpoint, request, cancellationToken);
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(LoginEndpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        }

        public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(RefreshEndpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        }

        public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            await _httpClient.PostAsJsonAsync(LogoutEndpoint, request, cancellationToken);
        }
    }
}