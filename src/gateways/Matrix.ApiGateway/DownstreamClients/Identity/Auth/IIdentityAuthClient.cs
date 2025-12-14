using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Auth
{
    public interface IIdentityAuthClient
    {
        Task<HttpResponseMessage> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> LoginAsync(
            LoginRequest request,
            string? clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> RefreshAsync(
            RefreshRequest request,
            string? clientIp,
            string? userAgent,
            CancellationToken cancellationToken = default);

        Task LogoutAsync(
            LogoutRequest request,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> GetSessionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> RevokeSessionAsync(
            Guid userId, Guid sessionId,
            CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> RevokeAllSessionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
