using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public interface IIdentityAuthClient
    {
        Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
        Task<HttpResponseMessage> LoginAsync(LoginRequest request, CancellationToken ct = default);

        Task<HttpResponseMessage> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
        Task LogoutAsync(RefreshRequest request, CancellationToken ct = default);

        Task<HttpResponseMessage> GetSessionsAsync(string authorizationHeader, CancellationToken ct = default);

        Task<HttpResponseMessage> RevokeSessionAsync(string authorizationHeader, Guid sessionId,
            CancellationToken ct = default);

        Task<HttpResponseMessage> RevokeAllSessionsAsync(string authorizationHeader, CancellationToken ct = default);
    }
}
