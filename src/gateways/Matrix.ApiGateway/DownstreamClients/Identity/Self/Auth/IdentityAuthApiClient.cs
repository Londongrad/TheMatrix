using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth
{
    public sealed class IdentityAuthApiClient(HttpClient httpClient) : IIdentityAuthClient
    {
        #region [ Fields ]

        private readonly HttpClient _httpClient = httpClient;

        #endregion [ Fields ]

        #region [ Methods ]

        public async Task<RegisterResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: RegisterEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<RegisterResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: RegisterEndpoint);
        }

        public async Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: LoginEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<LoginResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: LoginEndpoint);
        }

        public async Task<LoginResponse> RefreshAsync(
            RefreshRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: RefreshEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await resp.ReadJsonOrThrowDownstreamAsync<LoginResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: RefreshEndpoint);
        }

        public async Task LogoutAsync(
            LogoutRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage resp = await _httpClient.PostAsJsonAsync(
                requestUri: LogoutEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            await resp.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        #endregion [ Methods ]

        #region [ Constants ]

        private const string ServiceName = "Identity";

        private const string AuthBaseUrl = "/api/auth";

        private const string RegisterEndpoint = AuthBaseUrl + "/register";
        private const string LoginEndpoint = AuthBaseUrl + "/login";

        private const string LogoutEndpoint = AuthBaseUrl + "/logout";
        private const string RefreshEndpoint = AuthBaseUrl + "/refresh";

        private const string SessionsSegment = "/sessions";

        #endregion [ Constants ]
    }
}
