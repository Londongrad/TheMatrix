using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IRefreshTokenBulkRepository
    {
        Task<int> RevokeAllByUserIdAsync(
            Guid userId,
            RefreshTokenRevocationReason reason,
            CancellationToken ct);

        Task<int> RevokeByIdAsync(
            Guid userId,
            Guid refreshTokenId,
            RefreshTokenRevocationReason reason,
            CancellationToken ct);

        Task<int> DeleteExpiredAsync(
            DateTime utcNow,
            CancellationToken ct);

        Task<int> DeleteRevokedAndExpiredAsync(
            DateTime utcNow,
            CancellationToken ct);
    }
}
