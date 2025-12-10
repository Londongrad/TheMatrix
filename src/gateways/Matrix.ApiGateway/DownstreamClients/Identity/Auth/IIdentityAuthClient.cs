using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public interface IIdentityAuthClient
    {
        Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
        Task<HttpResponseMessage> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
        Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> GetSessionsAsync(string authorizationHeader,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> RevokeSessionAsync(string authorizationHeader, Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> RevokeAllSessionsAsync(string authorizationHeader,
            CancellationToken cancellationToken = default);
    }
}
