using Matrix.Identity.Contracts.Self.Sessions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions
{
    public interface IIdentitySessionsClient
    {
        Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(CancellationToken cancellationToken = default);

        Task RevokeSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task RevokeAllSessionsAsync(CancellationToken cancellationToken = default);
    }
}
