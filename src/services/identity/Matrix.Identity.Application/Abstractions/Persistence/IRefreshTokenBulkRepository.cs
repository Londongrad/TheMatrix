using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IRefreshTokenBulkRepository
    {
        Task<int> RevokeAllByUserIdAsync(
            Guid userId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken);

        Task<int> RevokeByIdAsync(
            Guid userId,
            Guid refreshTokenId,
            RefreshTokenRevocationReason reason,
            CancellationToken cancellationToken);

        Task<int> DeleteExpiredAsync(
            DateTime utcNow,
            CancellationToken cancellationToken);

        Task<int> DeleteRevokedAndExpiredAsync(
            DateTime utcNow,
            CancellationToken cancellationToken);
    }
}
