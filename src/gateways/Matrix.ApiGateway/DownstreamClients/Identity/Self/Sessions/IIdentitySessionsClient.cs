using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Self.Sessions.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions
{
    public interface IIdentitySessionsClient
    {
        Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<SessionResponse>> GetSessionHistoryPageAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task RevokeSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default);

        Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default);

        Task RevokeAllSessionsAsync(CancellationToken cancellationToken = default);
    }
}
