using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public sealed class IdentityAuthApiClient(HttpClient httpClient) : IIdentityAuthClient
    {
        #region [ Fields ]

        private readonly HttpClient _httpClient = httpClient;

        #endregion [ Fields ]

        #region [ Helpers ]

        private static void AddClientInfoHeaders(
            HttpRequestMessage request,
            string? clientIp,
            string? userAgent)
        {
            if (!string.IsNullOrWhiteSpace(clientIp))
                request.Headers.TryAddWithoutValidation(name: "X-Real-IP", value: clientIp);

            if (!string.IsNullOrWhiteSpace(userAgent))
            {
                request.Headers.Remove("User-Agent");
                request.Headers.TryAddWithoutValidation(name: "User-Agent", value: userAgent);
            }
        }

        #endregion [ Helpers ]

        #region [ Register & Login ]

        public async Task<HttpResponseMessage> RegisterAsync(RegisterRequest request,
            CancellationToken cancellationToken = default) =>
            await _httpClient.PostAsJsonAsync(requestUri: RegisterEndpoint, value: request,
                cancellationToken: cancellationToken);

        public async Task<HttpResponseMessage> LoginAsync(
            LoginRequest request,
            string? clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(method: HttpMethod.Post, requestUri: LoginEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            AddClientInfoHeaders(request: httpRequest, clientIp: clientIp, userAgent: userAgent);

            return await _httpClient.SendAsync(request: httpRequest, cancellationToken: cancellationToken);
        }

        #endregion [ Register & Login ]

        #region [ Refresh & Logout ]

        public async Task<HttpResponseMessage> RefreshAsync(
            RefreshRequest request,
            string? clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(method: HttpMethod.Post, requestUri: RefreshEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            AddClientInfoHeaders(request: httpRequest, clientIp: clientIp, userAgent: userAgent);

            return await _httpClient.SendAsync(request: httpRequest, cancellationToken: cancellationToken);
        }

        public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default) =>
            await _httpClient.PostAsJsonAsync(requestUri: LogoutEndpoint, value: request,
                cancellationToken: cancellationToken);

        #endregion [ Refresh & Logout ]

        #region [ Sessions ]

        public async Task<HttpResponseMessage> GetSessionsAsync(Guid userId,
            CancellationToken cancellationToken = default)
        {
            string endpoint = $"{AuthBaseUrl}/{userId} + {SessionsSegment}";

            using var request = new HttpRequestMessage(method: HttpMethod.Get, requestUri: endpoint);

            return await _httpClient.SendAsync(request: request, cancellationToken: cancellationToken);
        }

        public async Task<HttpResponseMessage> RevokeSessionAsync(Guid userId, Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            string endpoint = $"{AuthBaseUrl}/{userId} + {SessionsSegment}";

            using var request = new HttpRequestMessage(method: HttpMethod.Delete, requestUri: endpoint);

            return await _httpClient.SendAsync(request: request, cancellationToken: cancellationToken);
        }

        public async Task<HttpResponseMessage> RevokeAllSessionsAsync(Guid userId,
            CancellationToken cancellationToken = default)
        {
            string endpoint = $"{AuthBaseUrl}/{userId} + {SessionsSegment}";

            using var request = new HttpRequestMessage(method: HttpMethod.Delete, requestUri: endpoint);

            return await _httpClient.SendAsync(request: request, cancellationToken: cancellationToken);
        }

        #endregion [ Sessions ]

        #region [ Constants ]

        private const string AuthBaseUrl = "api/internal/auth";

        private const string RegisterEndpoint = AuthBaseUrl + "/register";
        private const string LoginEndpoint = AuthBaseUrl + "/login";

        private const string LogoutEndpoint = AuthBaseUrl + "/logout";
        private const string RefreshEndpoint = AuthBaseUrl + "/refresh";

        private const string SessionsSegment = "/sessions";

        #endregion [ Constants ]
    }
}
