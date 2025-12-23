using Matrix.Identity.Contracts.Sessions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Sessions
{
    public interface IIdentitySessionsClient
    {
        Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(CancellationToken ct = default);
        Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default);
        Task RevokeAllSessionsAsync(CancellationToken ct = default);
    }
}
